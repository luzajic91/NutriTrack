using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Features.FoodCatalog
{
    public record SearchFoodsQuery(
        string? Search,
        int? BrandId,
        int Page = 1,
        int PageSize = 20) : IRequest<PagedResult<FoodSummaryResponse>>;

    public record FoodSummaryResponse(
        int FoodId,
        string Name,
        string? BrandName,
        string? Description);

    public record PagedResult<T>(
        List<T> Items,
        int TotalCount,
        int Page,
        int PageSize)
    {
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    };

    public class SearchFoodsHandler : IRequestHandler<SearchFoodsQuery, PagedResult<FoodSummaryResponse>>
    {
        private readonly IAppDbContext _db;

        public SearchFoodsHandler(IAppDbContext db) => _db = db;

        public async Task<PagedResult<FoodSummaryResponse>> Handle(
            SearchFoodsQuery req, CancellationToken ct)
        {
            var query = _db.Foods
                .Include(f => f.Brand)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(req.Search))
                query = query.Where(f => f.Name.Contains(req.Search));

            if (req.BrandId.HasValue)
                query = query.Where(f => f.BrandId == req.BrandId);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(f => f.Name)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .Select(f => new FoodSummaryResponse(
                    f.FoodId,
                    f.Name,
                    f.Brand != null ? f.Brand.Name : null,
                    f.Description))
                .ToListAsync(ct);

            return new PagedResult<FoodSummaryResponse>(items, totalCount, req.Page, req.PageSize);
        }
    }
}
