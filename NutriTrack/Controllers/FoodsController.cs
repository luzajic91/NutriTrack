using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriTrack.Application.Features.FoodCatalog;

namespace NutriTrack.Api.Controllers
{
    [ApiController]
    [Route("api/foods")]
    [Authorize]
    public class FoodsController : ControllerBase
    {
        private readonly ISender _sender;

        public FoodsController(ISender sender) => _sender = sender;

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetFood(int id, CancellationToken ct)
        {
            var result = await _sender.Send(new GetFoodQuery(id), ct);
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
            var result = await _sender.Send(
                new SearchFoodsQuery(search, brandId, page, pageSize), ct);
            return Ok(result);
        }
    }
}
