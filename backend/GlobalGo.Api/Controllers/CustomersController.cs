using GlobalGo.Application.Interfaces;
using GlobalGo.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace GlobalGo.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICreditEvaluationService _service;

    public CustomersController(ICreditEvaluationService service)
    {
        _service = service;
    }

    [HttpGet("{dni}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerHistoryItemModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerHistoryItemModel>>> GetHistory(
        [FromRoute] string dni,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetCustomerHistoryAsync(dni, cancellationToken);
        return Ok(result.Select(x => x.ToApi()).ToList());
    }
}