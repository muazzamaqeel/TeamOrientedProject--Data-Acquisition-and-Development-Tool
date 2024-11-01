using System;
using System.Text;
using System.Text.Json;  // For JSON deserialization
using System.Threading.Tasks;
using Google.Protobuf;  // For Protobuf support
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;       // For MqttQualityOfServiceLevel
using Protos;  // Namespace for SensorData

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

            // Create the MQTT client without the verbose logger
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

        // Destructor (Finalizer) in case Dispose is not called manually
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
                .WithTcpServer(BROKER_ADDRESS, BROKER_PORT) // Connect to Docker Mosquitto
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
                throw; // Re-throw exception to be handled by caller
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
        private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                // Convert the payload to a JSON string
                var payloadJson = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                // Deserialize JSON payload to a dictionary
                var jsonData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);

                // Create a new Protobuf message instance
                var sensorData = new SensorData();

                // Set the topic
                sensorData.Topic = e.ApplicationMessage.Topic;

                // Check and populate PPG data fields
                if (jsonData.ContainsKey("led1") && jsonData.ContainsKey("led2") && jsonData.ContainsKey("led3"))
                {
                    sensorData.Led1 = jsonData["led1"].GetInt32();
                    sensorData.Led2 = jsonData["led2"].GetInt32();
                    sensorData.Led3 = jsonData["led3"].GetInt32();
                    sensorData.Temperature = jsonData.ContainsKey("temperature") ? jsonData["temperature"].GetSingle() : 0.0f;
                }

                // Check and populate IMU data fields
                if (jsonData.ContainsKey("acc_x") && jsonData.ContainsKey("gyro_x"))
                {
                    sensorData.AccX = jsonData["acc_x"].GetSingle();
                    sensorData.AccY = jsonData.ContainsKey("acc_y") ? jsonData["acc_y"].GetSingle() : 0.0f;
                    sensorData.AccZ = jsonData.ContainsKey("acc_z") ? jsonData["acc_z"].GetSingle() : 0.0f;
                    sensorData.GyroX = jsonData["gyro_x"].GetSingle();
                    sensorData.GyroY = jsonData.ContainsKey("gyro_y") ? jsonData["gyro_y"].GetSingle() : 0.0f;
                    sensorData.GyroZ = jsonData.ContainsKey("gyro_z") ? jsonData["gyro_z"].GetSingle() : 0.0f;
                    sensorData.MagX = jsonData.ContainsKey("mag_x") ? jsonData["mag_x"].GetSingle() : 0.0f;
                    sensorData.MagY = jsonData.ContainsKey("mag_y") ? jsonData["mag_y"].GetSingle() : 0.0f;
                    sensorData.MagZ = jsonData.ContainsKey("mag_z") ? jsonData["mag_z"].GetSingle() : 0.0f;
                }

                // Log or process specific fields
                Console.WriteLine($"Received data for topic: {sensorData.Topic}");

                // Display IMU data if present
                if (sensorData.Topic.Contains("imu"))
                {
                    Console.WriteLine($"Accelerometer Data - X: {sensorData.AccX}, Y: {sensorData.AccY}, Z: {sensorData.AccZ}");
                    Console.WriteLine($"Gyroscope Data - X: {sensorData.GyroX}, Y: {sensorData.GyroY}, Z: {sensorData.GyroZ}");
                    Console.WriteLine($"Magnetometer Data - X: {sensorData.MagX}, Y: {sensorData.MagY}, Z: {sensorData.MagZ}");
                }

                // Display PPG data if present
                if (sensorData.Topic.Contains("ppg"))
                {
                    Console.WriteLine($"LED Data - LED1: {sensorData.Led1}, LED2: {sensorData.Led2}, LED3: {sensorData.Led3}, Temperature: {sensorData.Temperature}");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse message: {ex.Message}");
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
