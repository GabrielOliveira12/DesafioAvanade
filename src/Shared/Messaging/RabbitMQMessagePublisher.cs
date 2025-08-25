using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Shared.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(string exchange, string routingKey, T message);
    }

    public class RabbitMQMessagePublisher : IMessagePublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMQMessagePublisher(string connectionString)
        {
            var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public async Task PublishAsync<T>(string exchange, string routingKey, T message)
        {
            _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
