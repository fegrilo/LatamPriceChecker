using LatamPriceChecker.Data;
using LatamPriceChecker.Models;
using LatamPriceChecker.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LatamPriceChecker.Tests.Repositories;

public class MonitoredItemRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoItemsExist()
    {
        await using var db = CreateContext();
        var repo = new MonitoredItemRepository(db);

        var items = await repo.GetAllAsync();

        Assert.Empty(items);
    }

    [Fact]
    public async Task CreateAsync_PersistsItem_AndAssignsId()
    {
        await using var db = CreateContext();
        var repo = new MonitoredItemRepository(db);

        var created = await repo.CreateAsync(new MonitoredItem { SearchWord = "espada", TargetPrice = 1000 });

        Assert.True(created.Id > 0);
        Assert.Equal("espada", created.SearchWord);
        Assert.Equal(1000, created.TargetPrice);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsItemsOrderedById()
    {
        await using var db = CreateContext();
        var repo = new MonitoredItemRepository(db);

        await repo.CreateAsync(new MonitoredItem { SearchWord = "item2", TargetPrice = 200 });
        await repo.CreateAsync(new MonitoredItem { SearchWord = "item1", TargetPrice = 100 });

        var items = await repo.GetAllAsync();

        Assert.Equal(2, items.Count);
        Assert.True(items[0].Id < items[1].Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsItem_WhenIdExists()
    {
        await using var db = CreateContext();
        var repo = new MonitoredItemRepository(db);
        var created = await repo.CreateAsync(new MonitoredItem { SearchWord = "espada", TargetPrice = 1000 });

        var found = await repo.GetByIdAsync(created.Id);

        Assert.NotNull(found);
        Assert.Equal("espada", found!.SearchWord);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        await using var db = CreateContext();
        var repo = new MonitoredItemRepository(db);

        var found = await repo.GetByIdAsync(999);

        Assert.Null(found);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesSearchWordAndTargetPrice_WhenIdExists()
    {
        await using var db = CreateContext();
        var repo = new MonitoredItemRepository(db);
        var created = await repo.CreateAsync(new MonitoredItem { SearchWord = "espada", TargetPrice = 1000 });

        var updated = await repo.UpdateAsync(created.Id, "machado", 2000);

        Assert.NotNull(updated);
        Assert.Equal("machado", updated!.SearchWord);
        Assert.Equal(2000, updated.TargetPrice);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges_VisibleOnSubsequentGet()
    {
        await using var db = CreateContext();
        var repo = new MonitoredItemRepository(db);
        var created = await repo.CreateAsync(new MonitoredItem { SearchWord = "espada", TargetPrice = 1000 });

        await repo.UpdateAsync(created.Id, "machado", 2000);
        var reloaded = await repo.GetByIdAsync(created.Id);

        Assert.Equal("machado", reloaded!.SearchWord);
        Assert.Equal(2000, reloaded.TargetPrice);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        await using var db = CreateContext();
        var repo = new MonitoredItemRepository(db);

        var updated = await repo.UpdateAsync(999, "machado", 2000);

        Assert.Null(updated);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_AndRemovesItem_WhenIdExists()
    {
        await using var db = CreateContext();
        var repo = new MonitoredItemRepository(db);
        var created = await repo.CreateAsync(new MonitoredItem { SearchWord = "espada", TargetPrice = 1000 });

        var deleted = await repo.DeleteAsync(created.Id);
        var found = await repo.GetByIdAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenIdDoesNotExist()
    {
        await using var db = CreateContext();
        var repo = new MonitoredItemRepository(db);

        var deleted = await repo.DeleteAsync(999);

        Assert.False(deleted);
    }
}
