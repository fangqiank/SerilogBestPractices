using MediatR;

namespace SerilogBestPractices.Events;

// 订单支付事件处理器：生成收据
public class OrderPaidReceiptHandler(
    ILogger<OrderPaidReceiptHandler> logger
    ) : INotificationHandler<OrderPaidEvent>
{
    public async Task Handle(OrderPaidEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Generating receipt for order {OrderId} with amount {Amount}",
            notification.OrderId,
            notification.Amount);

        await Task.Delay(80, cancellationToken); // 模拟收据生成耗时

        var receiptNumber = $"RCP-{notification.OrderId:N}".Substring(0, 12);

        logger.LogInformation(
            "Receipt {ReceiptNumber} generated for order {OrderId}",
            receiptNumber,
            notification.OrderId);
    }
}
