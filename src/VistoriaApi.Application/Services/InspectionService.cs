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
        var duplicatedCodes = request.PhotoRequirements
            .GroupBy(item => item.Code.Trim().ToLowerInvariant())
            .Any(group => group.Count() > 1);

        if (duplicatedCodes)
        {
            throw new AppException("Os códigos das fotos não podem se repetir.");
        }

        var template = new InspectionTemplate(
            _currentUser.UserId,
            request.Name,
            request.Category,
            request.Description);

        foreach (var requirement in request.PhotoRequirements.OrderBy(item => item.SortOrder))
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

    public Task<List<TemplateResponse>> ListTemplatesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Templates
            .AsNoTracking()
            .Include(template => template.Requirements)
            .Where(template => template.OwnerId == _currentUser.UserId && template.Active)
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
        var templateExists = await _dbContext.Templates.AnyAsync(
            template => template.Id == request.TemplateId
                && template.OwnerId == _currentUser.UserId
                && template.Active,
            cancellationToken);

        if (!templateExists)
        {
            throw new AppException("Modelo de vistoria não encontrado.", 404);
        }

        var publicToken = _tokenService.GeneratePublicToken();
        var expiresAtUtc = DateTime.UtcNow.AddDays(request.LinkValidDays);
        var inspection = new Inspection(
            _currentUser.UserId,
            request.TemplateId,
            request.RecipientName,
            request.RecipientEmail,
            request.AssetIdentification,
            _tokenService.HashPublicToken(publicToken),
            expiresAtUtc);

        _dbContext.Add(inspection);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateInspectionResponse(inspection, publicToken, frontendUrl);
    }

    public Task<List<InspectionSummaryResponse>> ListInspectionsAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.Inspections
            .AsNoTracking()
            .Where(inspection => inspection.OwnerId == _currentUser.UserId)
            .OrderByDescending(inspection => inspection.CreatedAtUtc)
            .Join(
                _dbContext.Templates,
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
            .ToListAsync(cancellationToken);
    }

    public async Task SendAsync(
        Guid inspectionId,
        string publicToken,
        string frontendUrl,
        CancellationToken cancellationToken)
    {
        var inspection = await GetOwnedInspectionAsync(inspectionId, cancellationToken);
        inspection.EnsureAvailable();

        var informedTokenHash = _tokenService.HashPublicToken(publicToken);
        if (!string.Equals(
                informedTokenHash,
                inspection.PublicTokenHash,
                StringComparison.Ordinal))
        {
            throw new AppException("Token público inválido.");
        }

        var publicUrl = BuildPublicUrl(frontendUrl, publicToken);
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
        var currentInspection = await GetOwnedInspectionAsync(inspectionId, cancellationToken);

        if (currentInspection.Status == InspectionStatus.Completed)
        {
            throw new AppException("Não é possível regenerar uma vistoria concluída.", 409);
        }

        var publicToken = _tokenService.GeneratePublicToken();
        var expiresAtUtc = DateTime.UtcNow.AddDays(7);
        var replacement = new Inspection(
            currentInspection.OwnerId,
            currentInspection.TemplateId,
            currentInspection.RecipientName,
            currentInspection.RecipientEmail,
            currentInspection.AssetIdentification,
            _tokenService.HashPublicToken(publicToken),
            expiresAtUtc);

        _dbContext.Add(replacement);

        var publicUrl = BuildPublicUrl(frontendUrl, publicToken);
        await _emailSender.SendInspectionInviteAsync(
            replacement.RecipientEmail,
            replacement.RecipientName,
            publicUrl,
            replacement.ExpiresAtUtc,
            cancellationToken);

        replacement.MarkAsSent();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateInspectionResponse(replacement, publicToken, frontendUrl);
    }

    public async Task<PublicInspectionResponse> GetPublicAsync(
        string publicToken,
        CancellationToken cancellationToken)
    {
        var inspection = await GetInspectionByPublicTokenAsync(publicToken, cancellationToken);

        if (inspection.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new AppException("O link da vistoria expirou.", 410);
        }

        var template = await _dbContext.Templates
            .AsNoTracking()
            .Include(item => item.Requirements)
            .SingleAsync(item => item.Id == inspection.TemplateId, cancellationToken);

        var requirements = template.Requirements
            .OrderBy(item => item.SortOrder)
            .Select(item => new PublicRequirementResponse(
                item.Id,
                item.Code,
                item.Label,
                item.Required,
                item.SortOrder,
                inspection.Photos.Any(photo => photo.RequirementId == item.Id)));

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
        ValidatePhoto(contentType, sizeBytes);

        var inspection = await GetInspectionByPublicTokenAsync(publicToken, cancellationToken);
        inspection.EnsureAvailable();

        var requirementExists = await _dbContext.Templates
            .Where(template => template.Id == inspection.TemplateId)
            .SelectMany(template => template.Requirements)
            .AnyAsync(requirement => requirement.Id == requirementId, cancellationToken);

        if (!requirementExists)
        {
            throw new AppException("A foto não pertence a este modelo de vistoria.");
        }

        var storagePath = await _fileStorage.SaveAsync(
            photoStream,
            contentType,
            cancellationToken);

        inspection.AddOrReplacePhoto(requirementId, storagePath, contentType, sizeBytes);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(
        string publicToken,
        CompleteInspectionRequest request,
        CancellationToken cancellationToken)
    {
        var inspection = await GetInspectionByPublicTokenAsync(publicToken, cancellationToken);
        inspection.EnsureAvailable();

        var requiredPhotoIds = await _dbContext.Templates
            .Where(template => template.Id == inspection.TemplateId)
            .SelectMany(template => template.Requirements)
            .Where(requirement => requirement.Required)
            .Select(requirement => requirement.Id)
            .ToListAsync(cancellationToken);

        var uploadedPhotoIds = inspection.Photos.Select(photo => photo.RequirementId);
        if (requiredPhotoIds.Except(uploadedPhotoIds).Any())
        {
            throw new AppException(
                "Envie todas as fotos obrigatórias antes de concluir a vistoria.");
        }

        inspection.Complete(request.Notes);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Inspection> GetOwnedInspectionAsync(
        Guid inspectionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Inspections.SingleOrDefaultAsync(
            inspection => inspection.Id == inspectionId
                && inspection.OwnerId == _currentUser.UserId,
            cancellationToken)
            ?? throw new AppException("Vistoria não encontrada.", 404);
    }

    private async Task<Inspection> GetInspectionByPublicTokenAsync(
        string publicToken,
        CancellationToken cancellationToken)
    {
        var publicTokenHash = _tokenService.HashPublicToken(publicToken);

        return await _dbContext.Inspections
            .Include(inspection => inspection.Photos)
            .SingleOrDefaultAsync(
                inspection => inspection.PublicTokenHash == publicTokenHash,
                cancellationToken)
            ?? throw new AppException("Link de vistoria inválido.", 404);
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

    private static string BuildPublicUrl(string frontendUrl, string publicToken)
    {
        return $"{frontendUrl.TrimEnd('/')}/vistoria/{publicToken}";
    }

    private static void ValidatePhoto(string contentType, long sizeBytes)
    {
        if (sizeBytes <= 0 || sizeBytes > MaximumPhotoSize)
        {
            throw new AppException("A foto deve ter no máximo 10 MB.", 413);
        }

        if (!AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new AppException("Formato aceito: JPEG, PNG ou WEBP.", 415);
        }
    }
}
