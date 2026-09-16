using System.ComponentModel.DataAnnotations;

namespace VistoriaApi.Api.Requests;

public sealed class UploadInspectionPhotoRequest
{
    [Required]
    public IFormFile Photo { get; init; } = null!;
}
