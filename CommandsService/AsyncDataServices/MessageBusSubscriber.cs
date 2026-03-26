using System.Text;
using CommandsService.EventProcessing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CommandsService.AsyncDataServices
{
    // BackgroundService makes this class run as a hosted background worker
    // for the lifetime of the application.
    public class MessageBusSubscriber : BackgroundService, IAsyncDisposable
    {
        private readonly IConfiguration _configuration;

        // Service responsible for deciding what event was received
        // and what business logic should run for that event.
        private readonly IEventProcessor _eventProcessor;

        // RabbitMQ TCP connection to the broker.
        // Nullable because it is created later during initialization, not in the constructor.
        private IConnection? _connection;

        // RabbitMQ channel created on top of the connection.
        // This is what we use for declaring exchanges, queues, bindings, and consuming messages.
        private IChannel? _channel;

        // Name of the queue that gets created and bound to the exchange.
        // In this case it will usually be a server-generated temporary queue name.
        private string? _queueName;

        public MessageBusSubscriber(IConfiguration configuration, IEventProcessor eventProcessor)
        {
            _configuration = configuration;
            _eventProcessor = eventProcessor;
        }

        // Creates RabbitMQ connection/channel, declares exchange,
        // declares queue, and binds queue to exchange.
        // This prepares the subscriber so it can start receiving messages.
        private async Task InitializeRabbitMQ(CancellationToken cancellationToken)
        {
            // Creates connection factory using configuration values.
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQHost"],
                Port = int.Parse(_configuration["RabbitMQPort"]!)
            };

            // Open a TCP connection to RabbitMQ broker.
            _connection = await factory.CreateConnectionAsync(cancellationToken: cancellationToken);

            // Create a channel on top of the connection.
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // Declare the exchange.
            // If it already exists with same settings, RabbitMQ keeps it.
            await _channel.ExchangeDeclareAsync(
                exchange: "trigger",
                type: ExchangeType.Fanout,
                cancellationToken: cancellationToken);

            // Declare a queue.
            // With no queue name supplied, RabbitMQ creates a temporary, auto-generated queue name.
            var queueResult = await _channel.QueueDeclareAsync(cancellationToken: cancellationToken);
            _queueName = queueResult.QueueName;

            // Bind the queue to the exchange so messages published to "trigger"
            // will be routed to this queue.
            await _channel.QueueBindAsync(
                queue: _queueName,
                exchange: "trigger",
                routingKey: "",
                cancellationToken: cancellationToken);

            Console.WriteLine("--> Listening on the MessageBus...");

            // Subscribe to connection shutdown event so we can log if connection closes.
            _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
        }

        // This is the main background worker method.
        // ASP.NET Core host calls this when application starts.
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Set up RabbitMQ infrastructure before consuming.
            await InitializeRabbitMQ(stoppingToken);

            // If cancellation was requested during startup, stop immediately.
            stoppingToken.ThrowIfCancellationRequested();

            // Safety check: make sure channel and queue were initialized successfully.
            if (_channel is null || string.IsNullOrWhiteSpace(_queueName))
            {
                throw new InvalidOperationException("RabbitMQ subscriber was not initialized correctly.");
            }

            // Async consumer that receives messages from RabbitMQ.
            var consumer = new AsyncEventingBasicConsumer(_channel);

            // This event runs whenever a message is delivered to the queue.
            consumer.ReceivedAsync += (sender, ea) =>
            {
                Console.WriteLine("--> Event received!");

                // Extract raw bytes from message body.
                var body = ea.Body;

                // Convert message bytes to string.
                var notificationMessage = Encoding.UTF8.GetString(body.ToArray());

                // Pass the event payload to the application's event processor.
                _eventProcessor.ProcessEvent(notificationMessage);

                return Task.CompletedTask;
            };

            // Start consuming messages from the queue.
            // autoAck: true means RabbitMQ considers the message handled
            // as soon as it delivers it to this consumer.
            await _channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: true,
                consumer: consumer,
                cancellationToken: stoppingToken);

            // Keep this background service alive until the app shuts down.
            // Without this, ExecuteAsync would end and the worker would stop.
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        // Runs when RabbitMQ connection shuts down.
        // Right now it only logs the shutdown event.
        private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs e)
        {
            Console.WriteLine("--> RabbitMQ Connection Shutdown");
            return Task.CompletedTask;
        }

        // Cleanup method for closing RabbitMQ resources gracefully.
        public async ValueTask DisposeAsync()
        {
            Console.WriteLine("--> MessageBus disposed");

            if (_channel is { IsOpen: true })
            {
                await _channel.CloseAsync();
            }

            if (_connection is { IsOpen: true })
            {
                await _connection.CloseAsync();
            }
        }
    }
}