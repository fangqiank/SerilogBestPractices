using MediatR;

namespace SerilogBestPractices.Events;

// 订单发货事件处理器：生成物流追踪号
public class OrderShippedTrackingHandler(
    ILogger<OrderShippedTrackingHandler> logger
    ) : INotificationHandler<OrderShippedEvent>
{
    public Task Handle(OrderShippedEvent notification, CancellationToken cancellationToken)
    {
        var trackingNumber = $"TRK-{Guid.NewGuid():N}".Substring(0, 12);

        logger.LogInformation(
            "Generated tracking number {TrackingNumber} for order {OrderId} shipped to {ShippingAddress}",
            trackingNumber,
            notification.OrderId,
            notification.ShippingAddress);

        return Task.CompletedTask;
    }
}
