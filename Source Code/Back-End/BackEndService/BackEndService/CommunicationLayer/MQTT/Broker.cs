using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Protos;
using SmartPacifier.BackEnd.CommunicationLayer.Protobuf;

namespace SmartPacifier.BackEnd.CommunicationLayer.MQTT
{
    public class Broker : IDisposable
    {
        private readonly string BROKER_ADDRESS = "localhost";
        private readonly int BROKER_PORT = 1883;

        private static Broker? _brokerInstance;
        private static readonly object _lock = new object();

        private readonly IMqttClient _mqttClient;
        private readonly ConcurrentQueue<(string Topic, byte[] Payload)> _messageQueue = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly SemaphoreSlim _semaphore = new(Environment.ProcessorCount * 2); // Limit concurrency.
        private const int MaxQueueSize = 1000; // Limit queue size to avoid memory overflow.
        private bool disposed = false;

        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

        private Broker()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;

            // Start background processing for queued messages.
            Task.Run(() => ProcessMessagesAsync(_cancellationTokenSource.Token));
        }

        public static Broker Instance
        {
            get
            {
                lock (_lock)
                {
                    return _brokerInstance ??= new Broker();
                }
            }
        }

        public async Task ConnectBroker()
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(BROKER_ADDRESS, BROKER_PORT)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(20))
                .WithCleanSession(false)
                .Build();

            try
            {
                await _mqttClient.ConnectAsync(options);
                Console.WriteLine("Successfully connected to Docker MQTT broker.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect to MQTT broker: " + ex.Message);
                throw;
            }
        }

        public async Task Subscribe(string topic)
        {
            var topicFilter = new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce) // Use AtLeastOnce QoS for reliability.
                .Build();

            var subscribeResult = await _mqttClient.SubscribeAsync(topicFilter);

            Console.WriteLine($"Subscribed to topic: {topic} with QoS {topicFilter.QualityOfServiceLevel}");
            foreach (var result in subscribeResult.Items)
            {
                Console.WriteLine($"Subscription result for topic '{result.TopicFilter.Topic}': {result.ResultCode}");
            }
        }

        private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var payload = e.ApplicationMessage.PayloadSegment.ToArray();
                var topic = e.ApplicationMessage.Topic;

                // Enqueue the message for background processing.
                if (_messageQueue.Count >= MaxQueueSize)
                {
                    _messageQueue.TryDequeue(out _); // Drop the oldest message.
                }
                _messageQueue.Enqueue((topic, payload));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enqueuing message: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_messageQueue.TryDequeue(out var message))
                {
                    await _semaphore.WaitAsync(cancellationToken);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessMessageAsync(message.Topic, message.Payload);
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    }, cancellationToken);
                }
                else
                {
                    await Task.Delay(10); // Prevent busy-waiting.
                }
            }
        }

        private async Task ProcessMessageAsync(string topic, byte[] payload)
        {
            try
            {
                Console.WriteLine($"Processing message from topic '{topic}'");

                string[] topicParts = topic.Split('/');
                if (topicParts.Length >= 2 && topicParts[0] == "Pacifier")
                {
                    var pacifierId = topicParts[1];
                    var (parsedPacifierId, parsedSensorType, parsedData) = ExposeSensorDataManager.Instance.ParseSensorData(payload);

                    if (parsedData != null)
                    {
                        Console.WriteLine($"Pacifier: {parsedPacifierId} - Sensor: {parsedSensorType}");
                        foreach (var sensorGroup in parsedData)
                        {
                            foreach (var kvp in sensorGroup)
                            {
                                Console.WriteLine($"     {kvp.Key}: {kvp.Value}");
                            }
                        }

                        MessageReceived?.Invoke(this, new MessageReceivedEventArgs(topic, payload, parsedPacifierId, parsedSensorType, parsedData));
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid topic format: {topic}");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process message: {ex.Message}");
            }
        }

        private Task OnConnectedAsync(MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("Connected successfully with MQTT Broker.");
            return Task.CompletedTask;
        }

        private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            Console.WriteLine("Disconnected from MQTT Broker.");

            await Task.Delay(TimeSpan.FromSeconds(5));
            try
            {
                await _mqttClient.ReconnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Reconnection failed: " + ex.Message);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                _mqttClient?.Dispose();
                _cancellationTokenSource.Cancel();
                _semaphore.Dispose();
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        public class MessageReceivedEventArgs : EventArgs
        {
            public string Topic { get; }
            public byte[] Payload { get; }
            public string PacifierId { get; }
            public string SensorType { get; }
            public ObservableCollection<Dictionary<string, object>> ParsedData { get; }

            public MessageReceivedEventArgs(string topic, byte[] payload, string pacifierId, string sensorType, ObservableCollection<Dictionary<string, object>> parsedData)
            {
                Topic = topic;
                Payload = payload;
                PacifierId = pacifierId;
                SensorType = sensorType;
                ParsedData = parsedData;
            }
        }
    }
}
