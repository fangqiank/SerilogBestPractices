using SerilogBestPractices.Models;

namespace SerilogBestPractices.Data;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order> CreateAsync(Order order);
    Task<Order> UpdateStatusAsync(Guid id, string status);
}
