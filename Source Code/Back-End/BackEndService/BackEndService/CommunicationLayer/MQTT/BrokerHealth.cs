using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using SmartPacifier.Interface.Services;

namespace SmartPacifier.BackEnd.CommunicationLayer.MQTT
{
    public class BrokerHealth : IBrokerHealthService
    {
        private readonly string brokerAddress = "localhost";
        private readonly int brokerPort = 1883;
        private IMqttClient mqttClient;
        private bool isReceivingMessages = false;

        public BrokerHealth()
        {
            // Initialize MQTT client
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();
        }

        /// <summary>
        /// Checks the overall broker health, including reachability and data reception.
        /// </summary>
        /// <returns>A string describing the broker's health status.</returns>
        public async Task<string> CheckBrokerHealthAsync()
        {
            bool isReachable = await IsBrokerReachableAsync();
            bool isReceiving = await IsReceivingDataAsync();

            if (isReachable && isReceiving)
            {
                return "Healthy: Broker is reachable and receiving data.";
            }
            else if (isReachable && !isReceiving)
            {
                return "Unhealthy: Broker is reachable but not receiving data.";
            }
            else
            {
                return "Unhealthy: Broker is not reachable.";
            }
        }

        /// <summary>
        /// Checks if the broker is reachable via a TCP connection.
        /// </summary>
        /// <returns>True if reachable, false otherwise.</returns>
        public async Task<bool> IsBrokerReachableAsync()
        {
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync(brokerAddress, brokerPort);
                    return tcpClient.Connected;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the broker is receiving data by subscribing to a topic.
        /// </summary>
        /// <returns>True if data is being received, false otherwise.</returns>
        public async Task<bool> IsReceivingDataAsync()
        {
            try
            {
                isReceivingMessages = false; // Reset flag

                // Configure MQTT client options dynamically
                var mqttClientOptions = new MqttFactory().CreateClientOptionsBuilder()
                    .WithTcpServer(brokerAddress, brokerPort)
                    .Build();

                // Ensure the client is connected
                if (!mqttClient.IsConnected)
                {
                    await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                }

                // Subscribe to the topic
                await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("Pacifier/#").Build());

                // Attach a message received handler
                mqttClient.ApplicationMessageReceivedAsync += async e =>
                {
                    if (e.ApplicationMessage != null && e.ApplicationMessage.Payload?.Length > 0)
                    {
                        isReceivingMessages = true; // Set flag when a valid message is received
                    }
                    await Task.CompletedTask;
                };

                // Wait for a short duration to allow messages to arrive
                await Task.Delay(2000);

                // Unsubscribe and disconnect the client (if necessary)
                await mqttClient.UnsubscribeAsync("Pacifier/#");
                await mqttClient.DisconnectAsync();

                // Return the result of the message receiving check
                return isReceivingMessages;
            }
            catch (Exception ex)
            {
                // Log the error and return false in case of an exception
                Console.WriteLine($"Error checking if data is being received: {ex.Message}");
                return false;
            }
        }

    }
}
