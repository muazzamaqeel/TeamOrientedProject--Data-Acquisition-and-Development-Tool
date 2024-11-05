using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using Google.Protobuf;
using Protos;

namespace SmartPacifier.BackEnd.CommunicationLayer.Protobuf
{
    public class ExposeSensorDataManager
    {
        private static readonly object _instanceLock = new object();
        private static ExposeSensorDataManager? _instance;

        private readonly List<SensorData> _sensorDataList = new List<SensorData>();
        private readonly object _dataLock = new object();

        public event EventHandler? SensorDataUpdated;

        private ExposeSensorDataManager() { }

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
        /// Parses sensor data based on the sensor type and pacifier ID.
        /// </summary>
        public IMessage? ParseDynamicSensorMessage(string pacifierId, string sensorType, byte[] data)
        {
            try
            {
                string jsonString = Encoding.UTF8.GetString(data);
                JsonDocument jsonDoc = JsonDocument.Parse(jsonString);

                IMessage message = sensorType.ToLower() switch
                {
                    "imu" => ParseImuData(jsonDoc),
                    "ppg" => ParsePpgData(jsonDoc),
                    _ => null
                };

                if (message != null)
                {
                    var sensorData = new SensorData
                    {
                        PacifierId = pacifierId
                    };

                    // Serialize the message to bytes
                    byte[] serializedData = message.ToByteArray();

                    // Add to sensor_data_map
                    sensorData.SensorDataMap.Add(sensorType, ByteString.CopyFrom(serializedData));

                    lock (_dataLock)
                    {
                        _sensorDataList.Add(sensorData);
                    }
                    SensorDataUpdated?.Invoke(this, EventArgs.Empty); // Trigger event to notify listeners

                    return message; // Return the actual sensor data message (IMUData or PPGData)
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse sensor data for Pacifier '{pacifierId}' and type '{sensorType}': {ex.Message}");
                return null;
            }
        }



        public void DisplayProtobufFields(IMessage message, int indentLevel = 0)
        {
            foreach (var field in message.Descriptor.Fields.InDeclarationOrder())
            {
                var value = field.Accessor.GetValue(message);
                string indent = new string(' ', indentLevel * 2);

                if (value is IMessage nestedMessage)
                {
                    Console.WriteLine($"{indent}{field.Name} (Message):");
                    DisplayProtobufFields(nestedMessage, indentLevel + 1);
                }
                else
                {
                    Console.WriteLine($"{indent}{field.Name}: {value}");
                }
            }
        }



        private IMessage ParseImuData(JsonDocument jsonDoc)
        {
            var imuData = new IMUData();
            foreach (var property in jsonDoc.RootElement.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "acc_x":
                        imuData.AccX = property.Value.GetSingle();
                        break;
                    case "acc_y":
                        imuData.AccY = property.Value.GetSingle();
                        break;
                    case "acc_z":
                        imuData.AccZ = property.Value.GetSingle();
                        break;
                    case "gyro_x":
                        imuData.GyroX = property.Value.GetSingle();
                        break;
                    case "gyro_y":
                        imuData.GyroY = property.Value.GetSingle();
                        break;
                    case "gyro_z":
                        imuData.GyroZ = property.Value.GetSingle();
                        break;
                    case "mag_x":
                        imuData.MagX = property.Value.GetSingle();
                        break;
                    case "mag_y":
                        imuData.MagY = property.Value.GetSingle();
                        break;
                    case "mag_z":
                        imuData.MagZ = property.Value.GetSingle();
                        break;
                }
            }
            return imuData;
        }

        private IMessage ParsePpgData(JsonDocument jsonDoc)
        {
            var ppgData = new PPGData();
            foreach (var property in jsonDoc.RootElement.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "led1":
                        ppgData.Led1 = property.Value.GetInt32();
                        break;
                    case "led2":
                        ppgData.Led2 = property.Value.GetInt32();
                        break;
                    case "led3":
                        ppgData.Led3 = property.Value.GetInt32();
                        break;
                    case "temperature":
                        ppgData.Temperature = property.Value.GetSingle();
                        break;
                }
            }
            return ppgData;
        }

        /// <summary>
        /// Retrieves a list of unique pacifier IDs from the stored sensor data.
        /// </summary>
        public List<string> GetPacifierIds()
        {
            lock (_dataLock)
            {
                return _sensorDataList.Select(data => data.PacifierId).Distinct().ToList();
            }
        }

        /// <summary>
        /// Retrieves all sensor data entries as a list.
        /// </summary>
        public List<SensorData> GetAllSensorData()
        {
            lock (_dataLock)
            {
                return _sensorDataList.Select(data => data.Clone()).ToList();
            }
        }

        /// <summary>
        /// Retrieves and displays unique pacifier names, showing a message if the list is empty.
        /// </summary>
        public List<string> GetPacifierNames()
        {
            lock (_dataLock)
            {
                if (!_sensorDataList.Any())
                {
                    MessageBox.Show("No data available in _sensorDataList.", "Debug", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return new List<string>();
                }

                var pacifierIds = _sensorDataList
                    .Where(data => !string.IsNullOrEmpty(data.PacifierId))
                    .Select(data => data.PacifierId)
                    .Distinct()
                    .ToList();

                if (!pacifierIds.Any())
                {
                    MessageBox.Show("PacifierIds are empty or null.", "Debug", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"PacifierIds: {string.Join(", ", pacifierIds)}", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                return pacifierIds;
            }
        }
    }
}
