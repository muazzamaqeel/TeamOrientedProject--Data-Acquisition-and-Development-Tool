using System;
using System.Collections.Generic;
using Protos;  // Ensure this is the namespace for your generated SensorData class

namespace SmartPacifier.BackEnd.CommunicationLayer.Protobuf
{
    /// <summary>
    /// Exposes functionality to manage and access SensorData globally.
    /// </summary>
    public class ExposeSensorDataManager
    {
        private static ExposeSensorDataManager? _instance;
        private SensorData _sensorData;

        // Private constructor to ensure singleton pattern
        private ExposeSensorDataManager()
        {
            _sensorData = LoadDefaultSensorData(); // Load default data
        }

        // Singleton instance for global access
        public static ExposeSensorDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ExposeSensorDataManager();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Loads default SensorData to be used initially.
        /// </summary>
        /// <returns>A new SensorData object with sample data.</returns>
        private SensorData LoadDefaultSensorData()
        {
            var sensorData = new SensorData();

            // Populate sensorData with dummy or default pacifiers
            for (int i = 1; i <= 5; i++)
            {
                sensorData.Pacifiers.Add(new PacifierData
                {
                    PacifierId = $"Pacifier {i}",
                    // Add other default properties if needed
                });
            }

            return sensorData;
        }

        /// <summary>
        /// Retrieves a list of pacifier names from the current SensorData.
        /// </summary>
        /// <returns>A list of pacifier names (IDs).</returns>
        public List<string> GetPacifierNames()
        {
            var pacifierNames = new List<string>();

            foreach (var pacifier in _sensorData.Pacifiers)
            {
                pacifierNames.Add(pacifier.PacifierId);
            }

            return pacifierNames;
        }

        /// <summary>
        /// Gets the current SensorData object, if needed for further processing.
        /// </summary>
        /// <returns>The current SensorData object managed by this class.</returns>
        public SensorData GetSensorData()
        {
            return _sensorData;
        }
    }
}
