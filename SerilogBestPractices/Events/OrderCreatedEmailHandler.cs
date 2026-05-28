using MediatR;
using SerilogBestPractices.Services;

namespace SerilogBestPractices.Events;

// 订单创建事件处理器：通过 IEmailService 发送确认邮件
public class OrderCreatedEmailHandler(
    IEmailService emailService
    ) : INotificationHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        await emailService.SendOrderConfirmationAsync(
            notification.Order.Id,
            notification.Order.CustomerName);
    }
}
