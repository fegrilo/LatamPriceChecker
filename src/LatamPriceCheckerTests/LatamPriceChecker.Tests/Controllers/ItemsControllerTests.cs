using LatamPriceChecker.Controllers;
using LatamPriceChecker.Models;
using LatamPriceChecker.Models.Dtos;
using LatamPriceChecker.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LatamPriceChecker.Tests.Controllers;

public class ItemsControllerTests
{
    private static (ItemsController controller, Mock<IMonitoredItemRepository> repo) CreateController()
    {
        var repo = new Mock<IMonitoredItemRepository>();
        var controller = new ItemsController(repo.Object);
        return (controller, repo);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithItems()
    {
        var (controller, repo) = CreateController();
        var items = new List<MonitoredItem>
        {
            new() { Id = 1, SearchWord = "espada", TargetPrice = 1000 }
        };
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var result = await controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(items, okResult.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenItemExists()
    {
        var (controller, repo) = CreateController();
        var item = new MonitoredItem { Id = 1, SearchWord = "espada", TargetPrice = 1000 };
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var result = await controller.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(item, okResult.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenItemDoesNotExist()
    {
        var (controller, repo) = CreateController();
        repo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((MonitoredItem?)null);

        var result = await controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenDtoIsValid()
    {
        var (controller, repo) = CreateController();
        var dto = new CreateMonitoredItemDto("espada", 1000);
        repo.Setup(r => r.CreateAsync(It.IsAny<MonitoredItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MonitoredItem item, CancellationToken _) =>
            {
                item.Id = 42;
                return item;
            });

        var result = await controller.Create(dto, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var createdItem = Assert.IsType<MonitoredItem>(createdResult.Value);
        Assert.Equal(42, createdItem.Id);
        Assert.Equal("espada", createdItem.SearchWord);
        Assert.Equal(nameof(ItemsController.GetById), createdResult.ActionName);
    }

    [Fact]
    public async Task Create_TrimsSearchWord_BeforePersisting()
    {
        var (controller, repo) = CreateController();
        var dto = new CreateMonitoredItemDto("  espada  ", 1000);
        MonitoredItem? capturedItem = null;
        repo.Setup(r => r.CreateAsync(It.IsAny<MonitoredItem>(), It.IsAny<CancellationToken>()))
            .Callback<MonitoredItem, CancellationToken>((item, _) => capturedItem = item)
            .ReturnsAsync((MonitoredItem item, CancellationToken _) => item);

        await controller.Create(dto, CancellationToken.None);

        Assert.Equal("espada", capturedItem!.SearchWord);
    }

    [Theory]
    [InlineData("", 1000)]
    [InlineData("   ", 1000)]
    [InlineData(null, 1000)]
    public async Task Create_ReturnsBadRequest_WhenSearchWordIsEmptyOrWhitespace(string? searchWord, long targetPrice)
    {
        var (controller, repo) = CreateController();
        var dto = new CreateMonitoredItemDto(searchWord!, targetPrice);

        var result = await controller.Create(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        repo.Verify(r => r.CreateAsync(It.IsAny<MonitoredItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Create_ReturnsBadRequest_WhenTargetPriceIsZeroOrNegative(long targetPrice)
    {
        var (controller, repo) = CreateController();
        var dto = new CreateMonitoredItemDto("espada", targetPrice);

        var result = await controller.Create(dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        repo.Verify(r => r.CreateAsync(It.IsAny<MonitoredItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenItemExistsAndDtoIsValid()
    {
        var (controller, repo) = CreateController();
        var dto = new UpdateMonitoredItemDto("machado", 2000);
        var updated = new MonitoredItem { Id = 1, SearchWord = "machado", TargetPrice = 2000 };
        repo.Setup(r => r.UpdateAsync(1, "machado", 2000, It.IsAny<CancellationToken>())).ReturnsAsync(updated);

        var result = await controller.Update(1, dto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(updated, okResult.Value);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenItemDoesNotExist()
    {
        var (controller, repo) = CreateController();
        var dto = new UpdateMonitoredItemDto("machado", 2000);
        repo.Setup(r => r.UpdateAsync(999, "machado", 2000, It.IsAny<CancellationToken>())).ReturnsAsync((MonitoredItem?)null);

        var result = await controller.Update(999, dto, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenDtoIsInvalid()
    {
        var (controller, repo) = CreateController();
        var dto = new UpdateMonitoredItemDto("", 2000);

        var result = await controller.Update(1, dto, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        repo.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenItemExists()
    {
        var (controller, repo) = CreateController();
        repo.Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenItemDoesNotExist()
    {
        var (controller, repo) = CreateController();
        repo.Setup(r => r.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.Delete(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}