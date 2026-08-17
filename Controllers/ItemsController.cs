using LatamPriceChecker.Models;
using LatamPriceChecker.Models.Dtos;
using LatamPriceChecker.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace LatamPriceChecker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly IMonitoredItemRepository _repository;

        public ItemsController(IMonitoredItemRepository repository)
        {
            _repository = repository;
        }

        // GET /api/items
        [HttpGet]
        [ProducesResponseType(typeof(List<MonitoredItem>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<MonitoredItem>>> GetAll(CancellationToken ct)
        {
            var items = await _repository.GetAllAsync(ct);
            return Ok(items);
        }

        // GET /api/items/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(MonitoredItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MonitoredItem>> GetById(int id, CancellationToken ct)
        {
            var item = await _repository.GetByIdAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }

        // POST /api/items
        [HttpPost]
        [ProducesResponseType(typeof(MonitoredItem), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<MonitoredItem>> Create(CreateMonitoredItemDto dto, CancellationToken ct)
        {
            var validationError = Validate(dto.SearchWord, dto.TargetPrice);
            if (validationError is not null)
                return BadRequest(new { error = validationError });

            var created = await _repository.CreateAsync(new MonitoredItem
            {
                SearchWord = dto.SearchWord.Trim(),
                TargetPrice = dto.TargetPrice
            }, ct);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT /api/items/{id}
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(MonitoredItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MonitoredItem>> Update(int id, UpdateMonitoredItemDto dto, CancellationToken ct)
        {
            var validationError = Validate(dto.SearchWord, dto.TargetPrice);
            if (validationError is not null)
                return BadRequest(new { error = validationError });

            var updated = await _repository.UpdateAsync(id, dto.SearchWord.Trim(), dto.TargetPrice, ct);
            return updated is null ? NotFound() : Ok(updated);
        }

        // DELETE /api/items/{id}
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var deleted = await _repository.DeleteAsync(id, ct);
            return deleted ? NoContent() : NotFound();
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
