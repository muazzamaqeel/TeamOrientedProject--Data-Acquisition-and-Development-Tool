using SmartPacifier.Interface.Services;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SmartPacifier.BackEnd.CommunicationLayer.MQTT
{
    public class BrokerHealth: IBrokerHealthService
    {
        private readonly string brokerAddress = "localhost";
        private readonly int brokerPort = 1883;

        public async Task<string> CheckBrokerHealthAsync()
        {
            try
            {
                // Attempt to establish a connection to the broker
                using (var tcpClient = new TcpClient())
                {
                    var connectTask = tcpClient.ConnectAsync(brokerAddress, brokerPort);
                    var timeoutTask = Task.Delay(2000); // Timeout of 2 seconds

                    // Wait for either connection or timeout
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    if (completedTask == timeoutTask)
                    {
                        return "Unhealthy: Connection Timeout";
                    }

                    if (tcpClient.Connected)
                    {
                        return "Healthy: Broker is reachable";
                    }

                    return "Unhealthy: Unable to connect";
                }
            }
            catch (Exception ex)
            {
                return $"Unhealthy: {ex.Message}";
            }
        }
    }
}
