namespace NutriTrack.Api.Controllers;

[ApiController]
[Route("api/recipes")]
[Authorize]
public class RecipesController : ControllerBase
{
    private readonly RecipeService _recipes;

    public RecipesController(RecipeService recipes) => _recipes = recipes;

    [HttpPost]
    public async Task<IActionResult> CreateRecipe(CreateRecipeCommand cmd, CancellationToken ct)
    {
        var id = await _recipes.CreateRecipe(cmd, ct);
        return CreatedAtAction(nameof(GetRecipe), new { id }, id);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRecipe(int id, CancellationToken ct)
    {
        var result = await _recipes.GetRecipe(id, ct);
        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> ListMyRecipes(CancellationToken ct)
    {
        var result = await _recipes.ListMyRecipes(ct);
        return Ok(result);
    }

    [HttpGet("available")]
    public async Task<IActionResult> ListAvailableRecipes(CancellationToken ct)
    {
        var result = await _recipes.ListAvailableRecipes(ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRecipe(int id, CancellationToken ct)
    {
        await _recipes.DeleteRecipe(id, ct);
        return NoContent();
    }

    [HttpGet("{id:int}/nutrition")]
    public async Task<IActionResult> GetRecipeNutrition(int id, CancellationToken ct)
    {
        var result = await _recipes.GetRecipeNutrition(id, ct);
        return Ok(result);
    }
}
