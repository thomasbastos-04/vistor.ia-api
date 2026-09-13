using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VistoriaApi.Domain.Common;

namespace VistoriaApi.Domain.Entities;

[Table("users", Schema = "vistoria")]
public sealed class User : Entity
{
    private User()
    {
    }

    public User(string name, string email, string passwordHash)
    {
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
    }

    [Required]
    [StringLength(120)]
    public string Name { get; private set; } = null!;

    [Required]
    [EmailAddress]
    [StringLength(180)]
    public string Email { get; private set; } = null!;

    [Required]
    [StringLength(500)]
    public string PasswordHash { get; private set; } = null!;
}
