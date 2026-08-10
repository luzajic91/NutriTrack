namespace NutriTrack.Api.Controllers;

[ApiController]
[Route("api/foods")]
[Authorize]
public class FoodsController : ControllerBase
{
    private readonly FoodCatalogService _foods;

    public FoodsController(FoodCatalogService foods) => _foods = foods;

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetFood(int id, CancellationToken ct)
    {
        var result = await _foods.GetFood(id, ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> SearchFoods(
        [FromQuery] SearchFoodsRequest cmd, CancellationToken ct)
    {
        var result = await _foods.SearchFoods(cmd, ct);
        return Ok(result);
    }
}
