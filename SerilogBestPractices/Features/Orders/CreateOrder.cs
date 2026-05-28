using MediatR;
using SerilogBestPractices.Data;
using SerilogBestPractices.Events;
using SerilogBestPractices.Models;

namespace SerilogBestPractices.Features.Orders
{
    public record CreateOrderCommand(string CustomerName, decimal Amount) : IRequest<Order>;

    public class CreateOrderHandler(
        ILogger<CreateOrderHandler> logger,
        IMediator mediator,
        IOrderRepository repository
        ) : IRequestHandler<CreateOrderCommand, Order>
    {
        public async Task<Order> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Creating order for customer {CustomerName}", request.CustomerName);

            if (request.Amount <= 0)
                throw new ArgumentException("Order amount must be greater than zero");

            if (request.CustomerName.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Duplicate order detected for this customer");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerName = request.CustomerName,
                Amount = request.Amount,
                CreatedAt = DateTimeOffset.UtcNow,
                Status = OrderStatus.Created
            };

            await repository.CreateAsync(order);

            logger.LogInformation("Order {OrderId} created successfully", order.Id);

            // 最佳实践 #8：业务操作成功后发布领域事件（Publish-Subscribe 模式）
            await mediator.Publish(new OrderCreatedEvent(order), cancellationToken);

            return order;
        }
    }
}
