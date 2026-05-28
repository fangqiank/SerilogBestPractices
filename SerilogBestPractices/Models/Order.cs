namespace SerilogBestPractices.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Status { get; internal set; } = OrderStatus.Created;
    }

    public class CreateOrderRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
