using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VistoriaApi.Domain.Common;
using VistoriaApi.Domain.Enums;
using VistoriaApi.Domain.Exceptions;

namespace VistoriaApi.Domain.Entities;

[Table("inspections", Schema = "vistoria")]
public sealed class Inspection : Entity
{
    private readonly List<InspectionPhoto> _photos = [];

    private Inspection()
    {
    }

    public Inspection(
        Guid ownerId,
        Guid templateId,
        string recipientName,
        string recipientEmail,
        string assetIdentification,
        string publicTokenHash,
        DateTime expiresAtUtc)
    {
        OwnerId = ownerId;
        TemplateId = templateId;
        RecipientName = recipientName.Trim();
        RecipientEmail = recipientEmail.Trim().ToLowerInvariant();
        AssetIdentification = assetIdentification.Trim();
        PublicTokenHash = publicTokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    [Required]
    public Guid OwnerId { get; private set; }

    [ForeignKey(nameof(OwnerId))]
    public User Owner { get; private set; } = null!;

    [Required]
    public Guid TemplateId { get; private set; }

    [ForeignKey(nameof(TemplateId))]
    public InspectionTemplate Template { get; private set; } = null!;

    [Required]
    [StringLength(120)]
    public string RecipientName { get; private set; } = null!;

    [Required]
    [EmailAddress]
    [StringLength(180)]
    public string RecipientEmail { get; private set; } = null!;

    [Required]
    [StringLength(120)]
    public string AssetIdentification { get; private set; } = null!;

    [Required]
    [StringLength(64, MinimumLength = 64)]
    public string PublicTokenHash { get; private set; } = null!;

    [Required]
    public DateTime ExpiresAtUtc { get; private set; }

    [Required]
    public InspectionStatus Status { get; private set; } = InspectionStatus.Draft;

    public DateTime? SentAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    [StringLength(2000)]
    public string? Notes { get; private set; }

    public IReadOnlyCollection<InspectionPhoto> Photos => _photos;

    public void MarkAsSent()
    {
        EnsureAvailable();
        Status = InspectionStatus.Sent;
        SentAtUtc = DateTime.UtcNow;
    }

    public void AddOrReplacePhoto(
        Guid requirementId,
        string storagePath,
        string contentType,
        long sizeBytes)
    {
        EnsureAvailable();

        _photos.RemoveAll(photo => photo.RequirementId == requirementId);
        _photos.Add(new InspectionPhoto(Id, requirementId, storagePath, contentType, sizeBytes));
        Status = InspectionStatus.InProgress;
    }

    public void Complete(string? notes)
    {
        EnsureAvailable();

        Notes = notes?.Trim();
        Status = InspectionStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Expire()
    {
        Status = InspectionStatus.Expired;
        ExpiresAtUtc = DateTime.UtcNow;
    }

    public void EnsureAvailable()
    {
        if (Status == InspectionStatus.Completed)
        {
            throw new DomainException("A vistoria já foi concluída.");
        }

        if (ExpiresAtUtc <= DateTime.UtcNow)
        {
            Status = InspectionStatus.Expired;
            throw new DomainException("O link da vistoria expirou.");
        }
    }
}
