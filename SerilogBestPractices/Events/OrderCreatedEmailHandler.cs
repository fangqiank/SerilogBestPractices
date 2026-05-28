using MediatR;

namespace SerilogBestPractices.Events;

// 订单创建事件处理器：模拟发送确认邮件
public class OrderCreatedEmailHandler(
    ILogger<OrderCreatedEmailHandler> logger
    ) : INotificationHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Sending order confirmation email for order {OrderId} to customer {CustomerName}",
            notification.Order.Id,
            notification.Order.CustomerName);

        await Task.Delay(100, cancellationToken); // 模拟邮件发送耗时

        logger.LogInformation(
            "Order confirmation email sent for order {OrderId}",
            notification.Order.Id);
    }
}
