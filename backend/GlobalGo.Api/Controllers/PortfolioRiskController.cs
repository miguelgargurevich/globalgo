using GlobalGo.Application.Interfaces;
using GlobalGo.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace GlobalGo.Api.Controllers;

[ApiController]
[Route("api/portfolio-risk")]
public sealed class PortfolioRiskController : ControllerBase
{
    private readonly ICreditEvaluationService _service;

    public PortfolioRiskController(ICreditEvaluationService service)
    {
        _service = service;
    }

    [HttpGet("report")]
    [ProducesResponseType(typeof(PortfolioRiskReportModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<PortfolioRiskReportModel>> GetReport(CancellationToken cancellationToken)
    {
        var report = await _service.GetPortfolioRiskReportAsync(cancellationToken);
        return Ok(report.ToApi());
    }
}