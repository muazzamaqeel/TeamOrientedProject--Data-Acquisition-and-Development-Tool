using System;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Protobuf;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Protos;
using SmartPacifier.BackEnd.CommunicationLayer.Protobuf;

namespace SmartPacifier.BackEnd.CommunicationLayer.MQTT
{
    /// <summary>
    /// Broker Class using Singleton Pattern. Connects to the Docker Mosquitto broker.
    /// </summary>
    public class Broker : IDisposable
    {
        private readonly string BROKER_ADDRESS = "localhost";  // Docker Mosquitto broker address
        private readonly int BROKER_PORT = 1883;               // Default MQTT port

        private static Broker? _brokerInstance;
        private static readonly object _lock = new object();

        private IMqttClient _mqttClient;
        private bool disposed = false;

        // Event handler for received messages
        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

        // Constructor for the Broker class
        private Broker()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            // Set up event handlers
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
        }

        /// <summary>
        /// Dispose Method to cleanup resources.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                _mqttClient?.Dispose();
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~Broker()
        {
            Dispose();
        }

        /// <summary>
        /// Getting an Instance of the Broker. If there is no instance
        /// yet, it will create one. This is thread-safe for
        /// multithreading.
        /// </summary>
        public static Broker Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_brokerInstance == null)
                    {
                        _brokerInstance = new Broker();
                    }
                    return _brokerInstance;
                }
            }
        }

        // Connect to the MQTT broker
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

        // Subscribe to a specific topic
        public async Task Subscribe(string topic)
        {
            var topicFilter = new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce) // QoS 0
                .Build();

            var subscribeResult = await _mqttClient.SubscribeAsync(topicFilter);

            Console.WriteLine($"Subscribed to topic: {topic} with QoS {topicFilter.QualityOfServiceLevel}");
            foreach (var result in subscribeResult.Items)
            {
                Console.WriteLine($"Subscription result for topic '{result.TopicFilter.Topic}': {result.ResultCode}");
            }
        }

        // Send a message to a specific topic
        public async Task SendMessage(string topic, string message)
        {
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce) // QoS 0
                .Build();

            try
            {
                await _mqttClient.PublishAsync(mqttMessage);
                Console.WriteLine($"Message sent to topic: {topic}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to topic: {topic} - {ex.Message}");
            }
        }

        // Event handler for received messages
        // Raw Data
        /*
        private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                // Convert the payload to a JSON string (or raw string if needed)
                var rawPayload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                // Extract the topic and log the raw message
                string topic = e.ApplicationMessage.Topic;
                Console.WriteLine($"Received raw data on topic '{topic}': {rawPayload}");

                // Optionally, raise an event with the raw payload if needed by other parts of the application
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs(topic, rawPayload));

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process message: {ex.Message}");
            }
        }
        */

        // Event handler for received messages
        private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var rawPayload = e.ApplicationMessage.Payload;
                string topic = e.ApplicationMessage.Topic;
                Console.WriteLine($"Received raw data on topic '{topic}'");

                // Extract pacifier ID and sensor type from the topic
                string[] topicParts = topic.Split('/');
                if (topicParts.Length >= 3 && topicParts[0] == "Pacifier")
                {
                    string pacifierId = topicParts[1];
                    string sensorType = topicParts[2];

                    var message = ExposeSensorDataManager.Instance.ParseDynamicSensorMessage(pacifierId, sensorType, rawPayload);

                    if (message != null)
                    {
                        Console.WriteLine($"Parsed {sensorType} data for Pacifier {pacifierId}:");
                        ExposeSensorDataManager.Instance.DisplayProtobufFields(message);
                    }
                    else
                    {
                        Console.WriteLine($"Unknown sensor type '{sensorType}' for Pacifier {pacifierId}, displaying raw bytes.");
                        Console.WriteLine($"Raw data: {BitConverter.ToString(rawPayload)}");
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



        // Event handler for successful connection
        private async Task OnConnectedAsync(MqttClientConnectedEventArgs e)
        {
            Console.WriteLine("Connected successfully with MQTT Broker.");
            await Task.CompletedTask;
        }

        // Event handler for disconnection
        private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs e)
        {
            Console.WriteLine("Disconnected from MQTT Broker.");

            // Optionally, attempt to reconnect
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

        // Event arguments for received messages
        public class MessageReceivedEventArgs : EventArgs
        {
            public string Topic { get; set; }
            public string Payload { get; set; }

            public MessageReceivedEventArgs(string topic, string payload)
            {
                Topic = topic;
                Payload = payload;
            }
        }
    }
}
