using MediatR;

namespace SerilogBestPractices.Events;

public record OrderShippedEvent(Guid OrderId, string CustomerName, string ShippingAddress) : INotification;
