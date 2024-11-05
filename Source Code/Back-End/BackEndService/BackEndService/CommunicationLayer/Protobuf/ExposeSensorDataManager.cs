using System;
using System.Collections.Generic;
using System.Linq;
using Protos; // Ensure this namespace includes your generated SensorData classes

namespace SmartPacifier.BackEnd.CommunicationLayer.Protobuf
{
    /// <summary>
    /// Manages SensorData globally and notifies subscribers when data is updated.
    /// </summary>
    public class ExposeSensorDataManager
    {
        private static readonly object _instanceLock = new object(); // Singleton lock
        private static ExposeSensorDataManager? _instance;

        private readonly List<SensorData> _sensorDataList = new List<SensorData>();
        private readonly object _dataLock = new object(); // Data lock

        // Event to notify subscribers when SensorData is updated
        public event EventHandler? SensorDataUpdated;

        // Private constructor to ensure singleton pattern
        private ExposeSensorDataManager() { }

        // Singleton instance for global access
        public static ExposeSensorDataManager Instance
        {
            get
            {
                lock (_instanceLock)
                {
                    return _instance ??= new ExposeSensorDataManager();
                }
            }
        }

        /// <summary>
        /// Retrieves a list of all pacifier IDs currently stored.
        /// </summary>
        public List<string> GetPacifierIds()
        {
            lock (_dataLock)
            {
                return _sensorDataList.Select(data => data.PacifierId).Distinct().ToList();
            }
        }

        /// <summary>
        /// Gets a copy of all SensorData objects for further processing.
        /// </summary>
        public List<SensorData> GetAllSensorData()
        {
            lock (_dataLock)
            {
                // Return a deep copy of the list to avoid external modifications
                return _sensorDataList.Select(data => data.Clone()).ToList();
            }
        }

        /// <summary>
        /// Updates or adds sensor data based on the pacifier ID.
        /// </summary>
        /// <param name="sensorData">The new or updated sensor data.</param>
        public void UpdateSensorData(SensorData sensorData)
        {
            lock (_dataLock)
            {
                // Locate the sensor data by pacifier ID and update it, or add it if not found
                var existingData = _sensorDataList.FirstOrDefault(data => data.PacifierId == sensorData.PacifierId);

                if (existingData != null)
                {
                    // Update existing sensor data
                    existingData.ImuData = sensorData.ImuData;
                    existingData.PpgData = sensorData.PpgData;
                }
                else
                {
                    // Add new sensor data
                    _sensorDataList.Add(sensorData);
                }
            }
            // Raise the event to notify subscribers
            SensorDataUpdated?.Invoke(this, EventArgs.Empty);
        }


        public List<string> GetPacifierNames()
        {
            lock (_dataLock)
            {
                return _sensorDataList.Select(data => data.PacifierId).Distinct().ToList();
            }
        }

    }
}
