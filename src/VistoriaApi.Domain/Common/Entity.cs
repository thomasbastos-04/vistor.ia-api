using System.ComponentModel.DataAnnotations;

namespace VistoriaApi.Domain.Common;

public abstract class Entity
{
    [Key]
    public Guid Id { get; protected set; } = Guid.NewGuid();

    [Required]
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
}
