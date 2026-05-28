using MediatR;

namespace SerilogBestPractices.Events;

// 订单创建事件处理器：写入审计日志
public class OrderCreatedLogHandler(
    ILogger<OrderCreatedLogHandler> logger
    ) : INotificationHandler<OrderCreatedEvent>
{
    public Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order created audit log: OrderId={OrderId}, CustomerName={CustomerName}, Amount={Amount}, CreatedAt={CreatedAt}",
            notification.Order.Id,
            notification.Order.CustomerName,
            notification.Order.Amount,
            notification.Order.CreatedAt);

        return Task.CompletedTask;
    }
}
