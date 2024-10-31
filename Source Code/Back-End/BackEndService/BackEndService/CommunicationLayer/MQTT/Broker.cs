using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace SmartPacifier.BackEnd.CommunicationLayer.MQTT
{
    ///<summary>
    /// Broker Class using Singleton Pattern. Connects to the Docker Mosquitto broker.
    ///</summary>
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
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;
        }

        ///<summary>
        /// Dispose Method to cleanup resources.
        ///</summary>
        public void Dispose()
        {
            if (!disposed)
            {
                _mqttClient?.Dispose();
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        // Destructor (Finalizer) in case Dispose is not called manually
        ~Broker()
        {
            Dispose();
        }

        ///<summary>
        /// Getting an Instance of the Broker. If there is no instance
        /// yet, it will create one. This is thread-safe for
        /// multithreading.
        ///</summary>
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
                .WithTcpServer(BROKER_ADDRESS, BROKER_PORT) // Connect to Docker Mosquitto
                .Build();

            try
            {
                await _mqttClient.ConnectAsync(options);
                Console.WriteLine("Successfully connected to Docker MQTT broker.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect to MQTT broker: " + ex.Message);
                throw new Exception("The MQTT client is not connected.");
            }
        }


        // Subscribe to a specific topic
        public async Task Subscribe(string topic)
        {
            await _mqttClient.SubscribeAsync(
                new MqttTopicFilterBuilder().WithTopic(topic).Build());

            Console.WriteLine($"Subscribed to topic: {topic}");
        }

        // Send a message to a specific topic
        public async Task SendMessage(string topic, string message)
        {
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
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
        private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            var messageReceivedEventArgs = new MessageReceivedEventArgs(
                e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
            MessageReceived?.Invoke(this, messageReceivedEventArgs);
            return Task.CompletedTask;
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
