using MediatR;
using SerilogBestPractices.Data;
using SerilogBestPractices.Events;
using SerilogBestPractices.Models;

namespace SerilogBestPractices.Features.Orders;

public record PayOrderCommand(Guid OrderId, decimal Amount) : IRequest<Order>;

public class PayOrderHandler(
    ILogger<PayOrderHandler> logger,
    IMediator mediator,
    IOrderRepository repository
    ) : IRequestHandler<PayOrderCommand, Order>
{
    public async Task<Order> Handle(PayOrderCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing payment for order {OrderId} with amount {Amount}",
            request.OrderId, request.Amount);

        if (request.Amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero");

        var order = await repository.GetByIdAsync(request.OrderId)
            ?? throw new ArgumentException($"Order {request.OrderId} not found");

        if (order.Status != OrderStatus.Created)
            throw new ArgumentException($"Order {request.OrderId} is not in {OrderStatus.Created} status, current: {order.Status}");

        var paidOrder = await repository.UpdateStatusAsync(request.OrderId, OrderStatus.Paid);

        logger.LogInformation("Payment processed for order {OrderId}", paidOrder.Id);

        // 支付成功后发布领域事件
        await mediator.Publish(new OrderPaidEvent(paidOrder.Id, paidOrder.Amount, paidOrder.CustomerName), cancellationToken);

        return paidOrder;
    }
}
