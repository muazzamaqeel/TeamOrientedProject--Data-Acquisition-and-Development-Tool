using System;
using System.Text;
using System.Threading.Tasks;
using SmartPacifier.Interface.Services;
using Protos;
using SmartPacifier.BackEnd.CommunicationLayer.Protobuf;
using SmartPacifier.BackEnd.AlgorithmLayer;
using System.Windows;


namespace SmartPacifier.BackEnd.CommunicationLayer.MQTT
{
    public class BrokerMain : IBrokerMain
    {
        private static bool _isBrokerRunning = false;
        private readonly Broker _broker;

        public BrokerMain()
        {
            _broker = Broker.Instance;
            _broker.MessageReceived += OnMessageReceived;
        }

        public async Task StartAsync(string[] args)
        {
            StringBuilder debugLog = new StringBuilder();
            debugLog.AppendLine("Starting MQTT Client...");

            if (!_isBrokerRunning)
            {
                _isBrokerRunning = true;
                bool connected = false;
                int retryCount = 0;
                const int maxRetries = 5;

                while (!connected && retryCount < maxRetries)
                {
                    try
                    {
                        // Attempt to connect to the Docker Mosquitto broker
                        await _broker.ConnectBroker();
                        await _broker.Subscribe("Pacifier/#");
                        debugLog.AppendLine("Client connected and subscribed to 'Pacifier/#' topic.");
                        connected = true;
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        debugLog.AppendLine($"Connection attempt {retryCount} failed: {ex.Message}");
                        Console.WriteLine($"Connection attempt {retryCount} failed: {ex.Message}");
                        await Task.Delay(2000);  // Wait 2 seconds before retrying
                    }
                }

                if (!connected)
                {
                    debugLog.AppendLine("Failed to connect to the MQTT Broker after multiple attempts.");
                    Console.WriteLine("Failed to connect to the MQTT Broker after multiple attempts.");
                }
            }
            else
            {
                debugLog.AppendLine("Client is already running. Skipping duplicate start.");
            }

            Console.WriteLine(debugLog.ToString()); // Write logs to the console
        }


        private async void OnMessageReceived(object? sender, Broker.MessageReceivedEventArgs e)
        {
            try
            {
                // Parse the received message
                var topicParts = e.Topic.Split('/');
                string pacifierId = topicParts.Length > 1 ? topicParts[1] : "Unknown";

                var (parsedPacifierId, sensorType, parsedData) = ExposeSensorDataManager.Instance.ParseSensorData(e.Payload);

                if (parsedData != null)
                {
                    Console.WriteLine($"Parsed data for Pacifier {parsedPacifierId} on sensor type '{sensorType}':");
                    //foreach (var dataEntry in parsedData)
                    //{
                    //    Console.WriteLine($"{dataEntry.Key}: {dataEntry.Value}");
                    //}

                    foreach (var dataEntry in parsedData)
                    {
                        foreach (var kvp in dataEntry)
                        {
                            Console.WriteLine($"    {kvp.Key}: {kvp.Value}");
                        }
                    }
                }

                // Forward the parsed data to Python using SensorDataForwardingService
                try
                {
                    // Get the base directory dynamically
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                    // Construct the path to the Python script dynamically
                    string pythonScriptPath = System.IO.Path.Combine(
                        baseDirectory,
                        "Resources",
                        "OutputResources",
                        "PythonFiles",
                        "ExecutableScript",
                        "python1.py"
                    );

                    // Create an instance of SensorDataForwardingService
                    var forwardingService = new SensorDataForwardingService(pythonScriptPath);

                    // Forward the parsed data to the Python script (use await instead of Wait)
                    await forwardingService.ForwardAndProcessDataAsync(parsedPacifierId, sensorType, parsedData);

                    Console.WriteLine($"Forwarded data for Pacifier {parsedPacifierId} on sensor type '{sensorType}' to Python script.");
                }
                catch (Exception forwardEx)
                {
                    Console.WriteLine($"Error forwarding data for Pacifier {pacifierId} to Python script: {forwardEx.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing message on topic '{e.Topic}': {ex.Message}");
            }
        }





    }
}