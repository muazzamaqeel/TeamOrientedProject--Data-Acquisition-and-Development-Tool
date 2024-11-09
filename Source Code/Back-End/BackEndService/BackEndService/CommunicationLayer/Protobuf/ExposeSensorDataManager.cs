using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.Reflection;
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
        /// Parses sensor data dynamically without hardcoding any message type names.
        /// </summary>
        public (string pacifierId, string sensorType, Dictionary<string, object> parsedData) ParseSensorData(string pacifierId, byte[] data)
        {
            try
            {
                string jsonString = Encoding.UTF8.GetString(data);
                JsonDocument jsonDoc = JsonDocument.Parse(jsonString);

                var sensorData = new SensorData
                {
                    PacifierId = pacifierId
                };

                var parsedMessage = ParseDynamicMessage(jsonDoc);
                if (parsedMessage != null)
                {
                    string sensorType = parsedMessage.Descriptor.Name.ToUpper();
                    sensorData.DataMap.Add(sensorType, parsedMessage.ToByteString());

                    lock (_dataLock)
                    {
                        _sensorDataList.Add(sensorData);
                    }

                    SensorDataUpdated?.Invoke(this, EventArgs.Empty);

                    Console.WriteLine($"Parsed {sensorType} data for Pacifier {pacifierId}:");
                    DisplayProtobufFields(parsedMessage);

                    // Extract all the fields into a dictionary
                    var sensorFields = GetSensorData(parsedMessage);

                    // Return the result as a Tuple (pacifierId, sensorType, sensorFields)
                    return (pacifierId, sensorType, sensorFields);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse sensor data for Pacifier '{pacifierId}': {ex.Message}");
            }

            return (null, null, null);
        }

        /// <summary>
        /// Uses Protobuf reflection to parse JSON data into a dynamically created Protobuf message.
        /// </summary>
        private IMessage? ParseDynamicMessage(JsonDocument jsonDoc)
        {
            IMessage? parsedMessage = null;

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                     .Where(t => typeof(IMessage).IsAssignableFrom(t) && t != typeof(SensorData)))
            {
                if (Activator.CreateInstance(type) is IMessage instance)
                {
                    if (TryPopulateMessageFieldsUsingReflection(instance, jsonDoc))
                    {
                        parsedMessage = instance;
                        break;
                    }
                }
            }

            return parsedMessage;
        }

        /// <summary>
        /// Dynamically populates fields in a Protobuf message using Protobuf reflection.
        /// </summary>
        private bool TryPopulateMessageFieldsUsingReflection(IMessage message, JsonDocument jsonDoc)
        {
            bool hasPopulated = false;

            foreach (var property in jsonDoc.RootElement.EnumerateObject())
            {
                var field = message.Descriptor.FindFieldByName(property.Name);
                if (field != null)
                {
                    hasPopulated = true;
                    object value = field.FieldType switch
                    {
                        FieldType.Float => property.Value.GetSingle(),
                        FieldType.Int32 => property.Value.GetInt32(),
                        FieldType.String => property.Value.GetString(),
                        _ => null
                    };

                    if (value != null)
                    {
                        field.Accessor.SetValue(message, value);
                    }
                    else if (field.FieldType == FieldType.Message)
                    {
                        var nestedMessage = (IMessage)field.Accessor.GetValue(message);
                        var nestedJson = JsonDocument.Parse(property.Value.GetRawText());
                        TryPopulateMessageFieldsUsingReflection(nestedMessage, nestedJson);
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

        /// <summary>
        /// Extracts sensor data dynamically as a dictionary (field name and value).
        /// </summary>
        private Dictionary<string, object> GetSensorData(IMessage parsedMessage)
        {
            var sensorData = new Dictionary<string, object>();

            // Iterate over all fields in the parsedMessage
            foreach (var field in parsedMessage.Descriptor.Fields.InFieldNumberOrder())
            {
                // Dynamically retrieve the field value
                var fieldValue = field.Accessor.GetValue(parsedMessage);

                if (fieldValue != null)
                {
                    // Store field name (sensor type) and value in the dictionary
                    sensorData[field.Name] = fieldValue;
                }
            }

            return sensorData;
        }
    }
}
