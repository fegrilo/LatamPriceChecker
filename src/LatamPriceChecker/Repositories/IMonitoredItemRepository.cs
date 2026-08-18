using LatamPriceChecker.Models;

namespace LatamPriceChecker.Repositories
{
    public interface IMonitoredItemRepository
    {
        Task<List<MonitoredItem>> GetAllAsync(CancellationToken ct = default);

        Task<MonitoredItem?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<MonitoredItem> CreateAsync(MonitoredItem item, CancellationToken ct = default);

        Task<MonitoredItem?> UpdateAsync(int id, string searchWord, long targetPrice, CancellationToken ct = default);

        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
