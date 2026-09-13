using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VistoriaApi.Application.Contracts;
using VistoriaApi.Application.Services;

namespace VistoriaApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/inspections")]
public sealed class InspectionsController : ControllerBase
{
    private readonly InspectionService _inspectionService;
    private readonly IConfiguration _configuration;

    public InspectionsController(
        InspectionService inspectionService,
        IConfiguration configuration)
    {
        _inspectionService = inspectionService;
        _configuration = configuration;
    }

    private string FrontendUrl =>
        _configuration["App:FrontendUrl"] ?? "http://localhost:4200";

    [HttpGet]
    public async Task<ActionResult<List<InspectionSummaryResponse>>> List(
        CancellationToken cancellationToken)
    {
        var inspections = await _inspectionService.ListInspectionsAsync(cancellationToken);
        return Ok(inspections);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateInspectionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _inspectionService.CreateInspectionAsync(
            request,
            FrontendUrl,
            cancellationToken);

        return Created($"/api/inspections/{response.Id}", response);
    }

    [HttpPost("{inspectionId:guid}/send")]
    public async Task<IActionResult> Send(
        Guid inspectionId,
        SendInspectionRequest request,
        CancellationToken cancellationToken)
    {
        await _inspectionService.SendAsync(
            inspectionId,
            request.PublicToken,
            FrontendUrl,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{inspectionId:guid}/regenerate-link")]
    public async Task<ActionResult<CreatedInspectionResponse>> RegenerateLink(
        Guid inspectionId,
        CancellationToken cancellationToken)
    {
        var response = await _inspectionService.RegenerateAndSendAsync(
            inspectionId,
            FrontendUrl,
            cancellationToken);

        return Ok(response);
    }
}
