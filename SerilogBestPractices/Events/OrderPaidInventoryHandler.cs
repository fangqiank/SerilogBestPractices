using MediatR;

namespace SerilogBestPractices.Events;

// 订单支付事件处理器：更新库存
public class OrderPaidInventoryHandler(
    ILogger<OrderPaidInventoryHandler> logger
    ) : INotificationHandler<OrderPaidEvent>
{
    public async Task Handle(OrderPaidEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Updating inventory for paid order {OrderId}",
            notification.OrderId);

        await Task.Delay(60, cancellationToken); // 模拟库存更新耗时

        logger.LogInformation(
            "Inventory updated for order {OrderId}, customer {CustomerName}",
            notification.OrderId,
            notification.CustomerName);
    }
}
