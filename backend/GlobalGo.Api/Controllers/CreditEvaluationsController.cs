using GlobalGo.Application.Interfaces;
using GlobalGo.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace GlobalGo.Api.Controllers;

[ApiController]
[Route("api/credit-evaluations")]
public sealed class CreditEvaluationsController : ControllerBase
{
    private readonly ICreditEvaluationService _service;

    public CreditEvaluationsController(ICreditEvaluationService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreditEvaluationResultModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreditEvaluationResultModel>> Evaluate(
        [FromBody] EvaluateCreditRequestModel request,
        CancellationToken cancellationToken)
    {
        var result = await _service.EvaluateAsync(request.ToApplication(), cancellationToken);
        return Ok(result.ToApi());
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<GeneralHistoryItemModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GeneralHistoryItemModel>>> GetGeneralHistory(
        [FromQuery] GeneralHistoryFilterModel filter,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetGeneralHistoryAsync(filter.ToApplication(), cancellationToken);
        return Ok(result.Select(x => x.ToApi()).ToList());
    }
}