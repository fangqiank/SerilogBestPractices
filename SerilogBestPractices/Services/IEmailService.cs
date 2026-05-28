namespace SerilogBestPractices.Services;

public interface IEmailService
{
    Task SendOrderConfirmationAsync(Guid orderId, string customerName);
}
