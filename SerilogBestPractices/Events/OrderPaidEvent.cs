using MediatR;

namespace SerilogBestPractices.Events;

public record OrderPaidEvent(Guid OrderId, decimal Amount, string CustomerName) : INotification;
