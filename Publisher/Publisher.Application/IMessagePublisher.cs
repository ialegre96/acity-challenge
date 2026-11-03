namespace Publisher.Application;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken);
}
