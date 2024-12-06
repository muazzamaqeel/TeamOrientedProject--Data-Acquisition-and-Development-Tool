using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using SmartPacifier.Interface.Services;
using Protos;
using SmartPacifier.BackEnd.CommunicationLayer.Protobuf;
using SmartPacifier.BackEnd.AlgorithmLayer;
using System.Windows;
using System.Collections.ObjectModel;

namespace SmartPacifier.BackEnd.CommunicationLayer.MQTT
{
    public class BrokerMain : IBrokerMain
    {
        private static bool isBrokerRunning = false;
        private readonly Broker broker;
        private readonly ConcurrentQueue<Broker.MessageReceivedEventArgs> messageQueue = new();
        private readonly SemaphoreSlim semaphore = new(Environment.ProcessorCount * 2); // Limit concurrency to double the CPU cores.
        private readonly CancellationTokenSource cancellationTokenSource = new();

        public BrokerMain()
        {
            broker = Broker.Instance;
            broker.MessageReceived += OnMessageReceived;
            Task.Run(() => ProcessMessagesAsync(cancellationTokenSource.Token)); // Start background processing.
        }

        public async Task StartAsync(string[] args)
        {
            StringBuilder debugLog = new StringBuilder();
            debugLog.AppendLine("Starting MQTT Client...");

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

        private void OnMessageReceived(object? sender, Broker.MessageReceivedEventArgs e)
        {
            if (messageQueue.Count >= 1000) // Limit the queue size to prevent memory issues.
            {
                messageQueue.TryDequeue(out _); // Drop the oldest message if the queue is full.
            }

            messageQueue.Enqueue(e); // Enqueue the received message for processing.
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (messageQueue.TryDequeue(out var message))
                {
                    await semaphore.WaitAsync(cancellationToken); // Limit concurrent processing.
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessMessageAsync(message);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken);
                }
                else
                {
                    await Task.Delay(10); // Small delay to prevent busy-waiting.
                }
            }
        }

        private async Task ProcessMessageAsync(Broker.MessageReceivedEventArgs e)
        {
            try
            {
                var (parsedPacifierId, sensorType, parsedData) = ExposeSensorDataManager.Instance.ParseSensorData(e.Payload);

                if (parsedData != null)
                {
                    Console.WriteLine($"Parsed data for Pacifier {parsedPacifierId} on sensor type '{sensorType}':");

                    foreach (var dataEntry in parsedData)
                    {
                        foreach (var kvp in dataEntry)
                        {
                            Console.WriteLine($"    {kvp.Key}: {kvp.Value}");
                        }
                    }
                }

                // Forward the parsed data to Python using SensorDataForwardingService
                await ForwardDataToPythonAsync(parsedPacifierId, sensorType, parsedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message on topic '{e.Topic}': {ex.Message}");
            }
        }

        private async Task ForwardDataToPythonAsync(string pacifierId, string sensorType, ObservableCollection<Dictionary<string, object>> parsedData)
        {
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
                await forwardingService.ForwardAndProcessDataAsync(pacifierId, sensorType, parsedData);

                Console.WriteLine($"Forwarded data for Pacifier {pacifierId} on sensor type '{sensorType}' to Python script.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error forwarding data for Pacifier {pacifierId} to Python script: {ex.Message}");
            }
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            semaphore.Dispose();
        }
    }
}
