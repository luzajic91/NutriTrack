namespace NutriTrack.Api.Controllers;

[ApiController]
[Route("api/foods")]
[Authorize]
public class FoodsController : ControllerBase
{
    private readonly Dispatcher _dispatcher;

    public FoodsController(Dispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetFood(int id, CancellationToken ct)
    {
        var result = await _dispatcher.Send(new GetFoodQuery(id), ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> SearchFoods(
        [FromQuery] string? search,
        [FromQuery] int? brandId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _dispatcher.Send(
            new SearchFoodsQuery(search, brandId, page, pageSize), ct);
        return Ok(result);
    }
}