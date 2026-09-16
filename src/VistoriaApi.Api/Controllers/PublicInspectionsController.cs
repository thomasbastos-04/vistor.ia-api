using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VistoriaApi.Api.Requests;
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
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> UploadPhoto(
        string publicToken,
        Guid requirementId,
        [FromForm] UploadInspectionPhotoRequest request,
        CancellationToken cancellationToken)
    {
        await using var photoStream = request.Photo.OpenReadStream();

        await _inspectionService.UploadPhotoAsync(
            publicToken,
            requirementId,
            photoStream,
            request.Photo.ContentType,
            request.Photo.Length,
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
