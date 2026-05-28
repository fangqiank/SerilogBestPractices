using MediatR;

namespace SerilogBestPractices.Events;

// 订单发货事件处理器：发送发货通知
public class OrderShippedNotificationHandler(
    ILogger<OrderShippedNotificationHandler> logger
    ) : INotificationHandler<OrderShippedEvent>
{
    public async Task Handle(OrderShippedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Sending shipping notification for order {OrderId} to customer {CustomerName} at {ShippingAddress}",
            notification.OrderId,
            notification.CustomerName,
            notification.ShippingAddress);

        await Task.Delay(90, cancellationToken); // 模拟通知发送耗时

        logger.LogInformation(
            "Shipping notification sent for order {OrderId}",
            notification.OrderId);
    }
}
