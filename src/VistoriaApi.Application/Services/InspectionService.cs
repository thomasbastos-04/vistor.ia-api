using Microsoft.EntityFrameworkCore;
using VistoriaApi.Application.Abstractions;
using VistoriaApi.Application.Contracts;
using VistoriaApi.Application.Exceptions;
using VistoriaApi.Domain.Entities;
using VistoriaApi.Domain.Enums;

namespace VistoriaApi.Application.Services;

public sealed class InspectionService
{
    private const long MaximumPhotoSize = 10 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    private readonly IAppDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ITokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly IFileStorage _fileStorage;

    public InspectionService(
        IAppDbContext dbContext,
        ICurrentUser currentUser,
        ITokenService tokenService,
        IEmailSender emailSender,
        IFileStorage fileStorage)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _fileStorage = fileStorage;
    }

    public async Task<Guid> CreateTemplateAsync(
        CreateTemplateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var duplicatedCodes = request.PhotoRequirements
            .GroupBy(
                requirement => requirement.Code.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);

        if (duplicatedCodes)
        {
            throw new AppException(
                "Os códigos das exigências fotográficas não podem se repetir.");
        }

        var template = new InspectionTemplate(
            _currentUser.UserId,
            request.Name,
            request.Category,
            request.Description);

        foreach (var requirement in request.PhotoRequirements
                     .OrderBy(requirement => requirement.SortOrder))
        {
            template.AddRequirement(
                requirement.Code,
                requirement.Label,
                requirement.Required,
                requirement.SortOrder);
        }

        _dbContext.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return template.Id;
    }

    public Task<List<TemplateResponse>> ListTemplatesAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.Templates
            .AsNoTracking()
            .Where(template =>
                template.OwnerId == _currentUser.UserId &&
                template.Active)
            .OrderByDescending(template => template.CreatedAtUtc)
            .Select(template => new TemplateResponse(
                template.Id,
                template.Name,
                template.Category,
                template.Description,
                template.Active,
                template.CreatedAtUtc,
                template.Requirements
                    .OrderBy(requirement => requirement.SortOrder)
                    .Select(requirement => new TemplateRequirementResponse(
                        requirement.Id,
                        requirement.Code,
                        requirement.Label,
                        requirement.Required,
                        requirement.SortOrder))))
            .ToListAsync(cancellationToken);
    }

    public async Task<CreatedInspectionResponse> CreateInspectionAsync(
        CreateInspectionRequest request,
        string frontendUrl,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var templateExists = await _dbContext.Templates
            .AsNoTracking()
            .AnyAsync(
                template =>
                    template.Id == request.TemplateId &&
                    template.OwnerId == _currentUser.UserId &&
                    template.Active,
                cancellationToken);

        if (!templateExists)
        {
            throw new AppException(
                "Modelo de vistoria não encontrado.",
                404);
        }

        var publicToken = _tokenService.GeneratePublicToken();
        var publicTokenHash = _tokenService.HashPublicToken(publicToken);
        var expiresAtUtc = DateTime.UtcNow.AddDays(request.LinkValidDays);

        var inspection = new Inspection(
            _currentUser.UserId,
            request.TemplateId,
            request.RecipientName,
            request.RecipientEmail,
            request.AssetIdentification,
            publicTokenHash,
            expiresAtUtc);

        _dbContext.Add(inspection);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateInspectionResponse(
            inspection,
            publicToken,
            frontendUrl);
    }

    public Task<List<InspectionSummaryResponse>> ListInspectionsAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.Inspections
            .AsNoTracking()
            .Where(inspection =>
                inspection.OwnerId == _currentUser.UserId)
            .Join(
                _dbContext.Templates.AsNoTracking(),
                inspection => inspection.TemplateId,
                template => template.Id,
                (inspection, template) => new InspectionSummaryResponse(
                    inspection.Id,
                    template.Name,
                    inspection.RecipientName,
                    inspection.RecipientEmail,
                    inspection.AssetIdentification,
                    inspection.Status,
                    inspection.ExpiresAtUtc,
                    inspection.CreatedAtUtc,
                    inspection.CompletedAtUtc))
            .OrderByDescending(inspection => inspection.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task SendAsync(
        Guid inspectionId,
        string publicToken,
        string frontendUrl,
        CancellationToken cancellationToken)
    {
        ValidatePublicToken(publicToken);

        var inspection = await GetOwnedInspectionAsync(
            inspectionId,
            cancellationToken);

        inspection.EnsureAvailable();

        var informedTokenHash =
            _tokenService.HashPublicToken(publicToken);

        if (!string.Equals(
                informedTokenHash,
                inspection.PublicTokenHash,
                StringComparison.Ordinal))
        {
            throw new AppException(
                "Token público inválido.",
                400);
        }

        var publicUrl = BuildPublicUrl(
            frontendUrl,
            publicToken);

        await _emailSender.SendInspectionInviteAsync(
            inspection.RecipientEmail,
            inspection.RecipientName,
            publicUrl,
            inspection.ExpiresAtUtc,
            cancellationToken);

        inspection.MarkAsSent();

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CreatedInspectionResponse> RegenerateAndSendAsync(
        Guid inspectionId,
        string frontendUrl,
        CancellationToken cancellationToken)
    {
        var currentInspection = await GetOwnedInspectionAsync(
            inspectionId,
            cancellationToken);

        if (currentInspection.Status == InspectionStatus.Completed)
        {
            throw new AppException(
                "Não é possível regenerar uma vistoria concluída.",
                409);
        }

        currentInspection.Expire();

        var publicToken = _tokenService.GeneratePublicToken();
        var publicTokenHash =
            _tokenService.HashPublicToken(publicToken);

        var expiresAtUtc = DateTime.UtcNow.AddDays(7);

        var replacement = new Inspection(
            currentInspection.OwnerId,
            currentInspection.TemplateId,
            currentInspection.RecipientName,
            currentInspection.RecipientEmail,
            currentInspection.AssetIdentification,
            publicTokenHash,
            expiresAtUtc);

        _dbContext.Add(replacement);

        var publicUrl = BuildPublicUrl(
            frontendUrl,
            publicToken);

        await _emailSender.SendInspectionInviteAsync(
            replacement.RecipientEmail,
            replacement.RecipientName,
            publicUrl,
            replacement.ExpiresAtUtc,
            cancellationToken);

        replacement.MarkAsSent();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateInspectionResponse(
            replacement,
            publicToken,
            frontendUrl);
    }

    public async Task<PublicInspectionResponse> GetPublicAsync(
        string publicToken,
        CancellationToken cancellationToken)
    {
        ValidatePublicToken(publicToken);

        var inspection = await GetInspectionByPublicTokenAsync(
            publicToken,
            cancellationToken);

        if (inspection.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new AppException(
                "O link da vistoria expirou.",
                410);
        }

        var template = await _dbContext.Templates
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == inspection.TemplateId,
                cancellationToken);

        if (template is null)
        {
            throw new AppException(
                "O modelo desta vistoria não foi encontrado.",
                404);
        }

        var requirements = template.Requirements
            .OrderBy(requirement => requirement.SortOrder)
            .Select(requirement => new PublicRequirementResponse(
                requirement.Id,
                requirement.Code,
                requirement.Label,
                requirement.Required,
                requirement.SortOrder,
                inspection.Photos.Any(
                    photo =>
                        photo.RequirementId == requirement.Id)))
            .ToList();

        return new PublicInspectionResponse(
            inspection.Id,
            template.Name,
            template.Category,
            inspection.RecipientName,
            inspection.AssetIdentification,
            inspection.Status,
            inspection.ExpiresAtUtc,
            requirements);
    }

    public async Task UploadPhotoAsync(
        string publicToken,
        Guid requirementId,
        Stream photoStream,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken)
    {
        ValidatePublicToken(publicToken);

        if (requirementId == Guid.Empty)
        {
            throw new AppException(
                "A exigência fotográfica não foi informada.",
                400);
        }

        ArgumentNullException.ThrowIfNull(photoStream);

        var normalizedContentType =
            NormalizeContentType(contentType);

        ValidatePhoto(
            normalizedContentType,
            sizeBytes);

        var inspection = await GetInspectionByPublicTokenAsync(
            publicToken,
            cancellationToken);

        inspection.EnsureAvailable();

        var requirementExists = await _dbContext.Templates
            .AsNoTracking()
            .Where(template =>
                template.Id == inspection.TemplateId)
            .SelectMany(template => template.Requirements)
            .AnyAsync(
                requirement =>
                    requirement.Id == requirementId,
                cancellationToken);

        if (!requirementExists)
        {
            throw new AppException(
                "A exigência fotográfica não pertence a esta vistoria.",
                400);
        }

        var previousPhoto = inspection.Photos
            .SingleOrDefault(
                photo =>
                    photo.RequirementId == requirementId);

        var storagePath = await _fileStorage.SaveAsync(
            photoStream,
            normalizedContentType,
            cancellationToken);

        inspection.AddOrReplacePhoto(
            requirementId,
            storagePath,
            normalizedContentType,
            sizeBytes);

        var persistedPhoto = inspection.Photos
            .Single(photo =>
                photo.RequirementId == requirementId);

        /*
         * Quando AddOrReplacePhoto cria uma entidade com Guid já preenchido,
         * o EF Core pode interpretá-la como Modified ao detectá-la somente
         * pela navegação. Registramos explicitamente apenas quando a entidade
         * atual é realmente nova.
         */
        if (!ReferenceEquals(previousPhoto, persistedPhoto))
        {
            _dbContext.Add(persistedPhoto);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(
        string publicToken,
        CompleteInspectionRequest request,
        CancellationToken cancellationToken)
    {
        ValidatePublicToken(publicToken);
        ArgumentNullException.ThrowIfNull(request);

        var inspection = await GetInspectionByPublicTokenAsync(
            publicToken,
            cancellationToken);

        inspection.EnsureAvailable();

        var requiredPhotoIds = await _dbContext.Templates
            .AsNoTracking()
            .Where(template =>
                template.Id == inspection.TemplateId)
            .SelectMany(template => template.Requirements)
            .Where(requirement => requirement.Required)
            .Select(requirement => requirement.Id)
            .ToListAsync(cancellationToken);

        var uploadedPhotoIds = inspection.Photos
            .Select(photo => photo.RequirementId)
            .ToHashSet();

        var missingRequiredPhotos = requiredPhotoIds
            .Where(requirementId =>
                !uploadedPhotoIds.Contains(requirementId))
            .ToList();

        if (missingRequiredPhotos.Count > 0)
        {
            throw new AppException(
                "Envie todas as fotos obrigatórias antes de concluir a vistoria.",
                400);
        }

        inspection.Complete(request.Notes);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Inspection> GetOwnedInspectionAsync(
        Guid inspectionId,
        CancellationToken cancellationToken)
    {
        if (inspectionId == Guid.Empty)
        {
            throw new AppException(
                "O identificador da vistoria é inválido.",
                400);
        }

        var inspection = await _dbContext.Inspections
            .SingleOrDefaultAsync(
                item =>
                    item.Id == inspectionId &&
                    item.OwnerId == _currentUser.UserId,
                cancellationToken);

        return inspection
            ?? throw new AppException(
                "Vistoria não encontrada.",
                404);
    }

    private async Task<Inspection> GetInspectionByPublicTokenAsync(
        string publicToken,
        CancellationToken cancellationToken)
    {
        var publicTokenHash =
            _tokenService.HashPublicToken(publicToken);

        var inspection = await _dbContext.Inspections
            .Include(item => item.Photos)
            .SingleOrDefaultAsync(
                item =>
                    item.PublicTokenHash == publicTokenHash,
                cancellationToken);

        return inspection
            ?? throw new AppException(
                "Link de vistoria inválido.",
                404);
    }

    private static CreatedInspectionResponse CreateInspectionResponse(
        Inspection inspection,
        string publicToken,
        string frontendUrl)
    {
        return new CreatedInspectionResponse(
            inspection.Id,
            BuildPublicUrl(frontendUrl, publicToken),
            publicToken,
            inspection.ExpiresAtUtc);
    }

    private static string BuildPublicUrl(
        string frontendUrl,
        string publicToken)
    {
        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            throw new InvalidOperationException(
                "A URL do frontend não foi configurada.");
        }

        ValidatePublicToken(publicToken);

        return $"{frontendUrl.TrimEnd('/')}/vistoria/{publicToken}";
    }

    private static void ValidatePublicToken(string publicToken)
    {
        if (string.IsNullOrWhiteSpace(publicToken))
        {
            throw new AppException(
                "O token público não foi informado.",
                400);
        }
    }

    private static string NormalizeContentType(
        string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new AppException(
                "O tipo do arquivo não foi informado.",
                415);
        }

        return contentType
            .Split(
                ';',
                2,
                StringSplitOptions.TrimEntries)
            [0]
            .ToLowerInvariant();
    }

    private static void ValidatePhoto(
        string contentType,
        long sizeBytes)
    {
        if (sizeBytes <= 0)
        {
            throw new AppException(
                "O arquivo enviado está vazio.",
                400);
        }

        if (sizeBytes > MaximumPhotoSize)
        {
            throw new AppException(
                "A foto deve ter no máximo 10 MB.",
                413);
        }

        if (!AllowedContentTypes.Contains(
                contentType,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new AppException(
                "Formato inválido. Envie uma imagem JPEG, PNG ou WEBP.",
                415);
        }
    }
}
