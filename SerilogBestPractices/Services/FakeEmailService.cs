using Microsoft.Extensions.Logging;

namespace SerilogBestPractices.Services;

// Demo 空实现：替代真实邮件服务（SendGrid / AWS SES / SMTP）
public class FakeEmailService(ILogger<FakeEmailService> logger) : IEmailService
{
    public async Task SendOrderConfirmationAsync(Guid orderId, string customerName)
    {
        logger.LogInformation(
            "Sending order confirmation email for order {OrderId} to customer {CustomerName}",
            orderId,
            customerName);

        await Task.Delay(100); // 模拟邮件发送耗时

        logger.LogInformation(
            "Order confirmation email sent for order {OrderId}",
            orderId);
    }
}
