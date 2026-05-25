using MediatR;
using SerilogBestPractices.Models;

namespace SerilogBestPractices.Features.Orders
{
    public record CreateOrderCommand(string CustomerName, decimal Amount) : IRequest<Order>;

    public class CreateOrderHandler(
        ILogger<CreateOrderHandler> logger
        ) : IRequestHandler<CreateOrderCommand, Order>
    {
        public async Task<Order> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Creating order for customer {CustomerName}", request.CustomerName);

            await Task.Delay(100, cancellationToken); // Simulate some work

            if (request.Amount <= 0)
                throw new ArgumentException("Order amount must be greater than zero");

            if (request.CustomerName.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Duplicate order detected for this customer");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = request.CustomerName,
                Amount = request.Amount,
                CreatedAt = DateTimeOffset.UtcNow
            };

            logger.LogInformation("Order {OrderId} created successfully", order.Id);

            return order;
        }
    }
}
