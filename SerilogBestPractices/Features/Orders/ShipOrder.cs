using MediatR;
using SerilogBestPractices.Data;
using SerilogBestPractices.Events;
using SerilogBestPractices.Models;

namespace SerilogBestPractices.Features.Orders;

public record ShipOrderCommand(Guid OrderId, string ShippingAddress) : IRequest<Order>;

public class ShipOrderHandler(
    ILogger<ShipOrderHandler> logger,
    IMediator mediator,
    IOrderRepository repository
    ) : IRequestHandler<ShipOrderCommand, Order>
{
    public async Task<Order> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Shipping order {OrderId} to {ShippingAddress}",
            request.OrderId, request.ShippingAddress);

        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
            throw new ArgumentException("Shipping address is required");

        var order = await repository.GetByIdAsync(request.OrderId)
            ?? throw new ArgumentException($"Order {request.OrderId} not found");

        if (order.Status != OrderStatus.Paid)
            throw new ArgumentException($"Order {request.OrderId} is not in {OrderStatus.Paid} status, current: {order.Status}");

        var shippedOrder = await repository.UpdateStatusAsync(request.OrderId, OrderStatus.Shipped);

        logger.LogInformation("Order {OrderId} shipped to {ShippingAddress}", shippedOrder.Id, request.ShippingAddress);

        // 发货成功后发布领域事件
        await mediator.Publish(new OrderShippedEvent(shippedOrder.Id, shippedOrder.CustomerName, request.ShippingAddress), cancellationToken);

        return shippedOrder;
    }
}
