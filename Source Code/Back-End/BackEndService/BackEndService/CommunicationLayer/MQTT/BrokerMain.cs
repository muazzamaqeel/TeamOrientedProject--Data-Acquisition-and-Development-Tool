using System;
using System.Text;
using System.Threading.Tasks;
using SmartPacifier.Interface.Services;

namespace SmartPacifier.BackEnd.CommunicationLayer.MQTT
{
    public class BrokerMain : IBrokerMain
    {
        private static bool isBrokerRunning = false;
        private readonly Broker broker;

        public BrokerMain()
        {
            broker = Broker.Instance;
            broker.MessageReceived += OnMessageReceived;
        }

        public async Task StartAsync(string[] args)
        {
            StringBuilder debugLog = new StringBuilder();
            debugLog.AppendLine("Starting MQTT Broker...");

            if (!isBrokerRunning)
            {
                isBrokerRunning = true;
                bool connected = false;
                int retryCount = 0;
                const int maxRetries = 5;

                while (!connected && retryCount < maxRetries)
                {
                    try
                    {
                        // Attempt to connect to the Docker Mosquitto broker
                        await broker.ConnectBroker();
                        await broker.Subscribe("Pacifier/#");
                        debugLog.AppendLine("Broker connected and subscribed to 'Pacifier/#' topic.");
                        connected = true;
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        debugLog.AppendLine($"Connection attempt {retryCount} failed: {ex.Message}");
                        await Task.Delay(2000);  // Wait 2 seconds before retrying
                    }
                }

                if (!connected)
                {
                    debugLog.AppendLine("Failed to connect to the MQTT Broker after multiple attempts.");
                }
            }
            else
            {
                debugLog.AppendLine("Broker is already running. Skipping duplicate start.");
            }

            Console.WriteLine(debugLog.ToString()); // Write logs to the console
        }

        private void OnMessageReceived(object? sender, Broker.MessageReceivedEventArgs e)
        {
            Console.WriteLine($"Received message on topic '{e.Topic}': {e.Payload}");
        }
    }
}
