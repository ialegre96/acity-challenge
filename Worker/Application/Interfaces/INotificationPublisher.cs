using Worker.Application.Messages;

namespace Worker.Application.Interfaces;

public interface INotificationPublisher
{
    Task PublishAsync(NotificacionMessage message, CancellationToken cancellationToken);
}
