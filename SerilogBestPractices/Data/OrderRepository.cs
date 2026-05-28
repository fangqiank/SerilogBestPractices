using Dapper;
using Microsoft.Data.Sqlite;
using SerilogBestPractices.Models;

namespace SerilogBestPractices.Data;

public class OrderRepository(SqliteConnection connection) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id)
    {
        var sql = "SELECT * FROM Orders WHERE Id = @Id";
        var row = await connection.QueryFirstOrDefaultAsync<OrderRow>(sql, new { Id = id.ToString() });

        if (row is null) return null;

        return new Order
        {
            Id = Guid.Parse(row.Id),
            CustomerName = row.CustomerName,
            Amount = row.Amount,
            CreatedAt = DateTimeOffset.Parse(row.CreatedAt),
            Status = row.Status
        };
    }

    public async Task<Order> CreateAsync(Order order)
    {
        var sql = """
            INSERT INTO Orders (Id, CustomerName, Amount, CreatedAt, Status)
            VALUES (@Id, @CustomerName, @Amount, @CreatedAt, @Status)
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = order.Id.ToString(),
            order.CustomerName,
            order.Amount,
            CreatedAt = order.CreatedAt.ToString("o"),
            Status = order.Status
        });

        return order;
    }

    public async Task<Order> UpdateStatusAsync(Guid id, string status)
    {
        var sql = "UPDATE Orders SET Status = @Status WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Status = status, Id = id.ToString() });
        return (await GetByIdAsync(id))!;
    }

    // Dapper 需要 property 名与列名匹配的内部类型
    private class OrderRow
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
