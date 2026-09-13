using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VistoriaApi.Application.Contracts;
using VistoriaApi.Application.Services;
using VistoriaApi.Domain.Entities;

namespace VistoriaApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/templates")]
public sealed class TemplatesController : ControllerBase
{
    private readonly InspectionService _inspectionService;

    public TemplatesController(InspectionService inspectionService)
    {
        _inspectionService = inspectionService;
    }

    [HttpGet]
    public async Task<ActionResult<List<InspectionTemplate>>> List(
        CancellationToken cancellationToken)
    {
        var templates = await _inspectionService.ListTemplatesAsync(cancellationToken);
        return Ok(templates);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var templateId = await _inspectionService.CreateTemplateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(nameof(List), new { id = templateId }, new { id = templateId });
    }
}
