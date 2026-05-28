using MediatR;
using SerilogBestPractices.Models;

namespace SerilogBestPractices.Events;

public record OrderCreatedEvent(Order Order) : INotification;
