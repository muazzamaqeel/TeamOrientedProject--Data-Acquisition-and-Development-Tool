using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Protos;
using Google.Protobuf.Reflection;


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
        /// Parses sensor data dynamically without hardcoding any message type names.
        /// </summary>
        public void ParseSensorData(string pacifierId, byte[] data)
        {
            try
            {
                string jsonString = Encoding.UTF8.GetString(data);
                JsonDocument jsonDoc = JsonDocument.Parse(jsonString);

                // Instantiate a generic SensorData message
                var sensorData = new SensorData
                {
                    PacifierId = pacifierId
                };

                // Dynamically determine the type of message to parse by examining jsonDoc
                var parsedMessage = ParseDynamicMessage(jsonDoc);
                if (parsedMessage != null)
                {
                    // Serialize the message to bytes
                    byte[] serializedData = parsedMessage.ToByteArray();

                    // Use reflection to determine the sensor type from the message class name
                    string sensorType = parsedMessage.Descriptor.Name.ToLower();
                    sensorData.SensorDataMap.Add(sensorType, ByteString.CopyFrom(serializedData));

                    lock (_dataLock)
                    {
                        _sensorDataList.Add(sensorData);
                    }

                    SensorDataUpdated?.Invoke(this, EventArgs.Empty); // Trigger event to notify listeners

                    Console.WriteLine($"Parsed {sensorType} data for Pacifier {pacifierId}:");
                    DisplayProtobufFields(parsedMessage); // Display parsed data
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse sensor data for Pacifier '{pacifierId}': {ex.Message}");
            }
        }

        /// <summary>
        /// Uses reflection to parse JSON data into a dynamically created Protobuf message.
        /// </summary>
        private IMessage? ParseDynamicMessage(JsonDocument jsonDoc)
        {
            IMessage? parsedMessage = null;

            // Attempt to parse as various known types by reflection
            foreach (var descriptor in typeof(SensorData).Assembly.GetTypes()
                         .Where(t => typeof(IMessage).IsAssignableFrom(t) && t != typeof(SensorData))
                         .Select(t => Activator.CreateInstance(t) as IMessage)
                         .Where(m => m != null))
            {
                if (TryPopulateMessageFields(descriptor!, jsonDoc))
                {
                    parsedMessage = descriptor;
                    break;
                }
            }

            return parsedMessage;
        }

        /// <summary>
        /// Dynamically populates fields in a Protobuf message based on JSON data.
        /// </summary>
        private bool TryPopulateMessageFields(IMessage message, JsonDocument jsonDoc)
        {
            bool hasPopulated = false;

            foreach (var property in jsonDoc.RootElement.EnumerateObject())
            {
                var field = message.Descriptor.FindFieldByName(property.Name);
                if (field != null)
                {
                    hasPopulated = true;
                    switch (field.FieldType)
                    {
                        case FieldType.Float:
                            field.Accessor.SetValue(message, property.Value.GetSingle());
                            break;
                        case FieldType.Int32:
                            field.Accessor.SetValue(message, property.Value.GetInt32());
                            break;
                        case FieldType.Message:
                            var nestedMessage = (IMessage)field.Accessor.GetValue(message);
                            if (nestedMessage != null)
                            {
                                var nestedJson = JsonDocument.Parse(property.Value.GetRawText());
                                TryPopulateMessageFields(nestedMessage, nestedJson);
                            }
                            break;
                    }
                }
            }

            return hasPopulated;
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
                    Console.WriteLine("No data available in _sensorDataList.");
                    return new List<string>();
                }

                var pacifierIds = _sensorDataList
                    .Where(data => !string.IsNullOrEmpty(data.PacifierId))
                    .Select(data => data.PacifierId)
                    .Distinct()
                    .ToList();

                if (!pacifierIds.Any())
                {
                    Console.WriteLine("PacifierIds are empty or null.");
                }
                else
                {
                    Console.WriteLine($"PacifierIds: {string.Join(", ", pacifierIds)}");
                }

                return pacifierIds;
            }
        }
    }
}
