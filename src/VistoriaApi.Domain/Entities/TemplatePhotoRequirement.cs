using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VistoriaApi.Domain.Common;

namespace VistoriaApi.Domain.Entities;

[Table("template_photo_requirements", Schema = "vistoria")]
public sealed class TemplatePhotoRequirement : Entity
{
    private TemplatePhotoRequirement()
    {
    }

    public TemplatePhotoRequirement(
        Guid templateId,
        string code,
        string label,
        bool required,
        int sortOrder)
    {
        TemplateId = templateId;
        Code = code.Trim().ToLowerInvariant();
        Label = label.Trim();
        Required = required;
        SortOrder = sortOrder;
    }

    [Required]
    public Guid TemplateId { get; private set; }

    [ForeignKey(nameof(TemplateId))]
    public InspectionTemplate Template { get; private set; } = null!;

    [Required]
    [StringLength(60)]
    public string Code { get; private set; } = null!;

    [Required]
    [StringLength(100)]
    public string Label { get; private set; } = null!;

    [Required]
    public bool Required { get; private set; }

    [Range(0, 999)]
    public int SortOrder { get; private set; }
}
