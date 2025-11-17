using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;

namespace censudex_orders.Services
{
    /// <summary>
    /// Interfaz para el servicio de mensajería RabbitMQ
    /// </summary>
    public interface IRabbitMQService
    {
        void PublishMessage<T>(string queueName, T message);
        void SubscribeToQueue(string queueName, Action<string> messageHandler);
        void Close();
    }

    /// <summary>
    /// Servicio para gestionar la comunicación con RabbitMQ
    /// Maneja la publicación y suscripción de mensajes
    /// </summary>
    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMQService> _logger;
        private readonly IConfiguration _configuration;

        public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            try
            {
                // Crear la conexión a RabbitMQ
                var factory = new ConnectionFactory()
                {
                    HostName = _configuration["RabbitMQ:HostName"],
                    Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName = _configuration["RabbitMQ:UserName"],
                    Password = _configuration["RabbitMQ:Password"],
                    VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/"
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declarar las colas necesarias
                DeclareQueues();

                _logger.LogInformation("Conexión a RabbitMQ establecida exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al conectar con RabbitMQ");
                throw;
            }
        }

        /// <summary>
        /// Declara todas las colas necesarias para el servicio
        /// </summary>
        private void DeclareQueues()
        {
            var queues = new[]
            {
                _configuration["RabbitMQ:Queues:OrderCreated"],
                _configuration["RabbitMQ:Queues:OrderStatusUpdated"],
                _configuration["RabbitMQ:Queues:OrderCancelled"],
                _configuration["RabbitMQ:Queues:StockValidation"],
                _configuration["RabbitMQ:Queues:StockFailed"]
            };

            foreach (var queue in queues)
            {
                if (!string.IsNullOrEmpty(queue))
                {
                    _channel.QueueDeclare(
                        queue: queue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null
                    );
                }
            }
        }

        /// <summary>
        /// Publica un mensaje en una cola específica
        /// </summary>
        /// <typeparam name="T">Tipo del mensaje</typeparam>
        /// <param name="queueName">Nombre de la cola</param>
        /// <param name="message">Mensaje a publicar</param>
        public void PublishMessage<T>(string queueName, T message)
        {
            try
            {
                var json = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true; // Mensaje persistente
                properties.ContentType = "application/json";
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: queueName,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation($"Mensaje publicado en cola {queueName}: {json}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al publicar mensaje en cola {queueName}");
                throw;
            }
        }

        /// <summary>
        /// Suscribe a una cola para recibir mensajes
        /// </summary>
        /// <param name="queueName">Nombre de la cola</param>
        /// <param name="messageHandler">Acción a ejecutar cuando se reciba un mensaje</param>
        public void SubscribeToQueue(string queueName, Action<string> messageHandler)
        {
            try
            {
                var consumer = new EventingBasicConsumer(_channel);
                
                consumer.Received += (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        
                        _logger.LogInformation($"Mensaje recibido de cola {queueName}: {message}");
                        
                        messageHandler(message);
                        
                        // Confirmar el procesamiento del mensaje
                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error al procesar mensaje de cola {queueName}");
                        // Rechazar el mensaje y reencolarlo
                        _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                _channel.BasicConsume(
                    queue: queueName,
                    autoAck: false, // Confirmación manual
                    consumer: consumer
                );

                _logger.LogInformation($"Suscrito a cola {queueName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al suscribirse a cola {queueName}");
                throw;
            }
        }

        /// <summary>
        /// Cierra la conexión con RabbitMQ
        /// </summary>
        public void Close()
        {
            _channel?.Close();
            _connection?.Close();
            _logger.LogInformation("Conexión a RabbitMQ cerrada");
        }

        public void Dispose()
        {
            Close();
        }
    }

    /// <summary>
    /// Modelos de mensajes para RabbitMQ
    /// </summary>
    public class OrderCreatedMessage
    {
        public Guid OrderId { get; set; }
        public Guid ClientId { get; set; }
        public List<OrderItemMessage> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class OrderItemMessage
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderStatusUpdatedMessage
    {
        public Guid OrderId { get; set; }
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class StockFailedMessage
    {
        public Guid OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<Guid> UnavailableProducts { get; set; } = new();
    }
}