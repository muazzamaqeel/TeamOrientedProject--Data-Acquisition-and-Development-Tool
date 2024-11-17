using SmartPacifier.Interface.Services;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

namespace SmartPacifier.BackEnd.CommunicationLayer.MQTT
{
    public class BrokerHealth : IBrokerHealthService
    {
        private readonly string brokerAddress = "localhost";
        private readonly int brokerPort = 1883;
        private bool isReceivingMessages = false; // Track if messages are being received

        public async Task<string> CheckBrokerHealthAsync()
        {
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    var connectTask = tcpClient.ConnectAsync(brokerAddress, brokerPort);
                    var timeoutTask = Task.Delay(2000); // Timeout of 2 seconds

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    if (completedTask == timeoutTask)
                    {
                        return "Unhealthy: Connection Timeout";
                    }

                    if (tcpClient.Connected)
                    {
                        var messageCheckResult = await CheckMessageReceivingAsync();
                        return messageCheckResult;
                    }

                    return "Unhealthy: Unable to connect";
                }
            }
            catch (Exception ex)
            {
                return $"Unhealthy: {ex.Message}";
            }
        }

        private async Task<string> CheckMessageReceivingAsync()
        {
            try
            {
                var mqttFactory = new MqttFactory();
                var mqttClient = mqttFactory.CreateMqttClient();

                // Reset the message receiving flag
                isReceivingMessages = false;

                mqttClient.ApplicationMessageReceivedAsync += async e =>
                {
                    isReceivingMessages = true;
                    await Task.CompletedTask;
                };

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(brokerAddress, brokerPort)
                    .WithCleanSession()
                    .Build();

                await mqttClient.ConnectAsync(options);
                await mqttClient.SubscribeAsync("Pacifier/#"); // Subscribe to a topic

                // Wait briefly to check for incoming messages
                await Task.Delay(2000);

                if (isReceivingMessages)
                {
                    await mqttClient.DisconnectAsync();
                    return "Healthy: Broker is reachable and receiving data";
                }
                else
                {
                    await mqttClient.DisconnectAsync();
                    return "Unhealthy: Connected but not receiving data";
                }
            }
            catch (Exception ex)
            {
                return $"Unhealthy: Failed to verify message receiving - {ex.Message}";
            }
        }
    }
}
