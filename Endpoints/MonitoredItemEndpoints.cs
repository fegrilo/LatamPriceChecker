using LatamPriceChecker.Models;
using LatamPriceChecker.Models.Dtos;
using LatamPriceChecker.Repositories;

namespace LatamPriceChecker.Endpoints
{
    public static class MonitoredItemEndpoints
    {
        public static void MapMonitoredItemEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/items").WithTags("MonitoredItems");

            group.MapGet("/", async (IMonitoredItemRepository repo, CancellationToken ct) =>
            {
                var items = await repo.GetAllAsync(ct);
                return Results.Ok(items);
            });

            group.MapGet("/{id:int}", async (int id, IMonitoredItemRepository repo, CancellationToken ct) =>
            {
                var item = await repo.GetByIdAsync(id, ct);
                return item is null ? Results.NotFound() : Results.Ok(item);
            });

            group.MapPost("/", async (CreateMonitoredItemDto dto, IMonitoredItemRepository repo, CancellationToken ct) =>
            {
                var validationError = Validate(dto.SearchWord, dto.TargetPrice);
                if (validationError is not null)
                    return Results.BadRequest(new { error = validationError });

                var created = await repo.CreateAsync(new MonitoredItem
                {
                    SearchWord = dto.SearchWord.Trim(),
                    TargetPrice = dto.TargetPrice
                }, ct);

                return Results.Created($"/api/items/{created.Id}", created);
            });

            group.MapPut("/{id:int}", async (int id, UpdateMonitoredItemDto dto, IMonitoredItemRepository repo, CancellationToken ct) =>
            {
                var validationError = Validate(dto.SearchWord, dto.TargetPrice);
                if (validationError is not null)
                    return Results.BadRequest(new { error = validationError });

                var updated = await repo.UpdateAsync(id, dto.SearchWord.Trim(), dto.TargetPrice, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            });

            group.MapDelete("/{id:int}", async (int id, IMonitoredItemRepository repo, CancellationToken ct) =>
            {
                var deleted = await repo.DeleteAsync(id, ct);
                return deleted ? Results.NoContent() : Results.NotFound();
            });
        }

        private static string? Validate(string searchWord, long targetPrice)
        {
            if (string.IsNullOrWhiteSpace(searchWord))
                return "SearchWord é obrigatório.";

            if (targetPrice <= 0)
                return "TargetPrice deve ser maior que zero.";

            return null;
        }
    }
}