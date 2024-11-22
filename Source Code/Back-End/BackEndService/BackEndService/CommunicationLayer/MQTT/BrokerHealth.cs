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
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();
        }

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

        public async Task<bool> IsBrokerReachableAsync()
        {
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync(brokerAddress, brokerPort);
                }

                // Attempt an MQTT handshake
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(brokerAddress, brokerPort)
                    .Build();

                if (!mqttClient.IsConnected)
                {
                    await mqttClient.ConnectAsync(options);
                }

                await mqttClient.DisconnectAsync(); // Disconnect after validation
                return true; // Successful MQTT connection means broker is reachable
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Broker reachability check failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsReceivingDataAsync()
        {
            try
            {
                isReceivingMessages = false; // Reset the flag

                var mqttClientOptions = new MqttFactory().CreateClientOptionsBuilder()
                    .WithTcpServer(brokerAddress, brokerPort)
                    .Build();

                if (!mqttClient.IsConnected)
                {
                    await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
                }

                // Subscribe to a topic
                await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("Pacifier/#").Build());

                mqttClient.ApplicationMessageReceivedAsync += async e =>
                {
                    if (e.ApplicationMessage.Payload?.Length > 0)
                    {
                        isReceivingMessages = true;
                    }
                    await Task.CompletedTask;
                };

                // Allow some time for messages to arrive
                await Task.Delay(2000);

                await mqttClient.UnsubscribeAsync("Pacifier/#");
                await mqttClient.DisconnectAsync();

                return isReceivingMessages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if data is being received: {ex.Message}");
                return false;
            }
        }
    }

}
