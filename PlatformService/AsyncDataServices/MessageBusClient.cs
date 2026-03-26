using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PlatformService.Dtos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PlatformService.AsyncDataServices
{
    public class MessageBusClient : IMessageBusClient, IAsyncDisposable
    {
        private readonly IConfiguration _configuration;
        private IConnection _connection;
        private IChannel _channel;

        public MessageBusClient(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task InitializeAsync()
        {
            // RabbitMQ Connection Factory
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQHost"],
                Port = int.Parse(_configuration["RabbitMQPort"]!)
            };

            try
            {
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
                await _channel.ExchangeDeclareAsync(exchange: "trigger", type: ExchangeType.Fanout);

                _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;

                Console.WriteLine("--> Connected to MessageBus");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not connect to the message bus : { ex.Message }");
            }
        }

        public async Task PublishNewPlatform(PlatformPublishedDto platformPublishedDto)
        {
            var message = JsonSerializer.Serialize(platformPublishedDto);

            if (_connection.IsOpen)
            {
                Console.WriteLine("--> RabbitMQ connection open, sending message...");
                await SendMessage(message);
            }
            else
            {
                Console.WriteLine("--> RabbitMQ connection is closed, not sending");
            }
        }

        private async Task SendMessage(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);

            await _channel.BasicPublishAsync(
                exchange: "trigger",
                routingKey: "",
                body: body
            );
            Console.WriteLine($"--> We have sent {message}");
        }

        private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs e)
        {
            Console.WriteLine("--> RabbitMQ Connection Shutdown");
            return Task.CompletedTask;
        }

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