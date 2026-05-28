using MediatR;
using SerilogBestPractices.Data;
using SerilogBestPractices.Models;

namespace SerilogBestPractices.Features.Orders
{
    public record GetOrderQuery(Guid OrderId) : IRequest<Order?>;

    public class GetOrderHandler(
        ILogger<GetOrderHandler> logger,
        IOrderRepository repository
        ) : IRequestHandler<GetOrderQuery, Order?>
    {
        public async Task<Order?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Querying order {OrderId}", request.OrderId);

            var order = await repository.GetByIdAsync(request.OrderId);

            if (order is not null)
                logger.LogInformation("Order {OrderId} retrieved successfully", request.OrderId);

            return order;
        }
    }
}
