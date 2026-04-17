namespace NutriTrack.Api.Controllers;

[ApiController]
[Route("api/recipes")]
[Authorize]
public class RecipesController : ControllerBase
{
    private readonly Dispatcher _dispatcher;

    public RecipesController(Dispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpPost]
    public async Task<IActionResult> CreateRecipe(CreateRecipeCommand cmd, CancellationToken ct)
    {
        var id = await _dispatcher.Send(cmd, ct);
        return CreatedAtAction(nameof(GetRecipe), new { id }, id);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRecipe(int id, CancellationToken ct)
    {
        var result = await _dispatcher.Send(new GetRecipeQuery(id), ct);
        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> ListMyRecipes(CancellationToken ct)
    {
        var result = await _dispatcher.Send(new ListMyRecipesQuery(), ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRecipe(int id, CancellationToken ct)
    {
        await _dispatcher.Send(new DeleteRecipeCommand(id), ct);
        return NoContent();
    }

    [HttpGet("{id:int}/nutrition")]
    public async Task<IActionResult> GetRecipeNutrition(int id, CancellationToken ct)
    {
        var result = await _dispatcher.Send(new GetRecipeNutritionQuery(id), ct);
        return Ok(result);
    }
}