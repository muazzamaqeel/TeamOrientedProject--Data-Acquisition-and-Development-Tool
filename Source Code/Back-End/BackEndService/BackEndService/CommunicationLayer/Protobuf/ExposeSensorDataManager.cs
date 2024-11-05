using System;
using System.Collections.Generic;
using System.Linq;
using Protos; // Make sure this namespace includes your generated SensorData classes

namespace SmartPacifier.BackEnd.CommunicationLayer.Protobuf
{
    /// <summary>
    /// Manages SensorData globally and notifies subscribers when data is updated.
    /// </summary>
    public class ExposeSensorDataManager
    {
        private static ExposeSensorDataManager? _instance;
        private readonly SensorData _sensorData;
        private readonly object _lock = new object();

        // Event to notify subscribers when SensorData is updated
        public event EventHandler? SensorDataUpdated;

        // Private constructor to ensure singleton pattern
        private ExposeSensorDataManager()
        {
            _sensorData = new SensorData();
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
        /// Retrieves a list of pacifier names from the current SensorData.
        /// </summary>
        /// <returns>A list of pacifier names (IDs).</returns>
        public List<string> GetPacifierNames()
        {
            lock (_lock)
            {
                return _sensorData.Pacifiers.Select(p => p.PacifierId).ToList();
            }
        }

        /// <summary>
        /// Gets the current SensorData object, if needed for further processing.
        /// </summary>
        /// <returns>The current SensorData object managed by this class.</returns>
        public SensorData GetSensorData()
        {
            lock (_lock)
            {
                return _sensorData;
            }
        }

        /// <summary>
        /// Updates or adds pacifier data and notifies subscribers.
        /// </summary>
        /// <param name="pacifierData">The new or updated pacifier data.</param>
        public void UpdatePacifierData(PacifierData pacifierData)
        {
            lock (_lock)
            {
                var existingPacifier = _sensorData.Pacifiers.FirstOrDefault(p => p.PacifierId == pacifierData.PacifierId);
                if (existingPacifier != null)
                {
                    // Update existing pacifier data
                    existingPacifier.ImuData = pacifierData.ImuData;
                    existingPacifier.PpgData = pacifierData.PpgData;
                }
                else
                {
                    // Add new pacifier
                    _sensorData.Pacifiers.Add(pacifierData);
                }
            }
            // Raise the event to notify subscribers
            SensorDataUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
