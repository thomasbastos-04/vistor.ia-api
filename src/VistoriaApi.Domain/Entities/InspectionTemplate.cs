using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VistoriaApi.Domain.Common;

namespace VistoriaApi.Domain.Entities;

[Table("inspection_templates", Schema = "vistoria")]
public sealed class InspectionTemplate : Entity
{
    private readonly List<TemplatePhotoRequirement> _requirements = [];

    private InspectionTemplate()
    {
    }

    public InspectionTemplate(Guid ownerId, string name, string category, string? description)
    {
        OwnerId = ownerId;
        Name = name.Trim();
        Category = category.Trim();
        Description = description?.Trim();
    }

    [Required]
    public Guid OwnerId { get; private set; }

    [ForeignKey(nameof(OwnerId))]
    public User Owner { get; private set; } = null!;

    [Required]
    [StringLength(120)]
    public string Name { get; private set; } = null!;

    [Required]
    [StringLength(80)]
    public string Category { get; private set; } = null!;

    [StringLength(500)]
    public string? Description { get; private set; }

    [Required]
    public bool Active { get; private set; } = true;

    public IReadOnlyCollection<TemplatePhotoRequirement> Requirements => _requirements;

    public void AddRequirement(string code, string label, bool required, int sortOrder)
    {
        _requirements.Add(new TemplatePhotoRequirement(Id, code, label, required, sortOrder));
    }
}
