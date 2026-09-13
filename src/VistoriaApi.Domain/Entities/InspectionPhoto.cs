using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VistoriaApi.Domain.Common;

namespace VistoriaApi.Domain.Entities;

[Table("inspection_photos", Schema = "vistoria")]
public sealed class InspectionPhoto : Entity
{
    private InspectionPhoto()
    {
    }

    public InspectionPhoto(
        Guid inspectionId,
        Guid requirementId,
        string storagePath,
        string contentType,
        long sizeBytes)
    {
        InspectionId = inspectionId;
        RequirementId = requirementId;
        StoragePath = storagePath;
        ContentType = contentType;
        SizeBytes = sizeBytes;
    }

    [Required]
    public Guid InspectionId { get; private set; }

    [ForeignKey(nameof(InspectionId))]
    public Inspection Inspection { get; private set; } = null!;

    [Required]
    public Guid RequirementId { get; private set; }

    [ForeignKey(nameof(RequirementId))]
    public TemplatePhotoRequirement Requirement { get; private set; } = null!;

    [Required]
    [StringLength(500)]
    public string StoragePath { get; private set; } = null!;

    [Required]
    [StringLength(100)]
    public string ContentType { get; private set; } = null!;

    [Range(1, 10_485_760)]
    public long SizeBytes { get; private set; }
}
