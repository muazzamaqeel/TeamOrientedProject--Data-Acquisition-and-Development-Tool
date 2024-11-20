using System;
using System.Text.Json;
using System.Threading.Tasks;
using SmartPacifier.BackEnd.AlgorithmLayer;
using SmartPacifier.BackEnd.CommunicationLayer.Protobuf;

namespace SmartPacifier.BackEnd.AlgorithmLayer
{
    public class SensorDataForwardingService
    {
        private readonly DataForwarder _dataForwarder;
        private readonly ExposeSensorDataManager _sensorDataManager;

        public SensorDataForwardingService(string pythonScriptPath)
        {
            _dataForwarder = new DataForwarder(pythonScriptPath);

            // Access the singleton instance of ExposeSensorDataManager
            _sensorDataManager = ExposeSensorDataManager.Instance;
        }

        public async Task ForwardAndProcessDataAsync(string pacifierId, string sensorType, object parsedData)
        {
            try
            {
                // Serialize parsed data into JSON
                var dataJson = JsonSerializer.Serialize(new
                {
                    PacifierId = pacifierId,
                    SensorType = sensorType,
                    Data = parsedData
                });

                // Forward the data to the Python script
                string response = await _dataForwarder.ForwardToPythonAsync(dataJson);
                Console.WriteLine($"Python Response: {response}");

                // Handle the response further if necessary
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in forwarding and processing data: {ex.Message}");
            }
        }

        public void UseSensorDataManagerForSomethingElse()
        {
            // Example: Retrieve and display pacifier IDs
            var pacifierIds = _sensorDataManager.GetPacifierIds();
            Console.WriteLine($"Available Pacifiers: {string.Join(", ", pacifierIds)}");
        }
    }
}
