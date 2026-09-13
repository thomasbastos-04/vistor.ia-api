using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VistoriaApi.Application.Contracts;
using VistoriaApi.Application.Services;

namespace VistoriaApi.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/inspections")]
public sealed class PublicInspectionsController : ControllerBase
{
    private readonly InspectionService _inspectionService;

    public PublicInspectionsController(InspectionService inspectionService)
    {
        _inspectionService = inspectionService;
    }

    [HttpGet("{publicToken}")]
    public async Task<ActionResult<PublicInspectionResponse>> Get(
        string publicToken,
        CancellationToken cancellationToken)
    {
        var response = await _inspectionService.GetPublicAsync(
            publicToken,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{publicToken}/photos/{requirementId:guid}")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> UploadPhoto(
        string publicToken,
        Guid requirementId,
        [FromForm] IFormFile photo,
        CancellationToken cancellationToken)
    {
        await using var photoStream = photo.OpenReadStream();

        await _inspectionService.UploadPhotoAsync(
            publicToken,
            requirementId,
            photoStream,
            photo.ContentType,
            photo.Length,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{publicToken}/complete")]
    public async Task<IActionResult> Complete(
        string publicToken,
        CompleteInspectionRequest request,
        CancellationToken cancellationToken)
    {
        await _inspectionService.CompleteAsync(
            publicToken,
            request,
            cancellationToken);

        return NoContent();
    }
}
