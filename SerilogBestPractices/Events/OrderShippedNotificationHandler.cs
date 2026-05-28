using MediatR;
using Microsoft.Extensions.Logging;

namespace SerilogBestPractices.Events;

// 订单发货事件处理器：模拟外部通知服务调用 + 瞬态故障处理
// 演示 best-effort 模式 — 通知失败不传播到主流程，仅记录日志
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

        try
        {
            await Task.Delay(90, cancellationToken); // 模拟通知服务调用耗时
            // 模拟外部服务瞬态故障（如 503 Service Unavailable）
            throw new HttpRequestException("Notification service returned 503 Service Unavailable");
        }
        catch (HttpRequestException ex)
        {
            // 最佳实践：通知类事件采用 best-effort 模式
            // 异常不传播到主流程，通过日志告警 + 后台重试机制处理
            logger.LogWarning(ex,
                "Shipping notification failed for order {OrderId}, queued for retry. Error: {Error}",
                notification.OrderId,
                ex.Message);
        }
    }
}
