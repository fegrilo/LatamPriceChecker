using LatamPriceChecker.Data;
using LatamPriceChecker.Models;
using Microsoft.EntityFrameworkCore;

namespace LatamPriceChecker.Repositories
{
    public class MonitoredItemRepository : IMonitoredItemRepository
    {
        private readonly AppDbContext _db;

        public MonitoredItemRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<MonitoredItem>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.MonitoredItems
                .AsNoTracking()
                .OrderBy(i => i.Id)
                .ToListAsync(ct);
        }

        public async Task<MonitoredItem?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _db.MonitoredItems
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id, ct);
        }

        public async Task<MonitoredItem> CreateAsync(MonitoredItem item, CancellationToken ct = default)
        {
            _db.MonitoredItems.Add(item);
            await _db.SaveChangesAsync(ct);
            return item;
        }

        public async Task<MonitoredItem?> UpdateAsync(int id, string searchWord, long targetPrice, CancellationToken ct = default)
        {
            var existing = await _db.MonitoredItems.FirstOrDefaultAsync(i => i.Id == id, ct);
            if (existing is null)
                return null;

            existing.SearchWord = searchWord;
            existing.TargetPrice = targetPrice;

            await _db.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var existing = await _db.MonitoredItems.FirstOrDefaultAsync(i => i.Id == id, ct);
            if (existing is null)
                return false;

            _db.MonitoredItems.Remove(existing);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
