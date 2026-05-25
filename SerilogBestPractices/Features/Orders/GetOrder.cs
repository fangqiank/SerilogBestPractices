using MediatR;
using SerilogBestPractices.Models;

namespace SerilogBestPractices.Features.Orders
{
    public record GetOrderQuery(Guid OrderId) : IRequest<Order?>;

    public class GetOrderHandler(
        ILogger<GetOrderHandler> logger
        ) : IRequestHandler<GetOrderQuery, Order?>
    {
        public async Task<Order?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Querying order {OrderId}", request.OrderId);

            await Task.Delay(50, cancellationToken);

            var order = new Order
            {
                Id = request.OrderId,
                CustomerName = "John Doe",
                Amount = 99.99m,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            logger.LogInformation("Order {OrderId} retrieved successfully", request.OrderId);

            return order;
        }
    }
}
