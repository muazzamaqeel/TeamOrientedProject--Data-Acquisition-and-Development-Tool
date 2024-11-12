using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using Protos;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;

namespace SmartPacifier.BackEnd.CommunicationLayer.Protobuf
{
    public class ExposeSensorDataManager
    {
        private static readonly object _instanceLock = new object();
        private static ExposeSensorDataManager? _instance;

        private readonly List<SensorData> _sensorDataList = new List<SensorData>();
        private readonly object _dataLock = new object();

        // Event to notify subscribers when sensor data is updated
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

        public (string pacifierId, string sensorType, Dictionary<string, object> parsedData) ParseSensorData(string pacifierId, string topic, byte[] data)
        {
            var parsedData = new Dictionary<string, object>();
            string detectedSensorType = topic.Split('/').Last();

            try
            {
                var sensorData = SensorData.Parser.ParseFrom(data);
                sensorData.SensorType = detectedSensorType;

                // Get the message type corresponding to sensorData.SensorType
                Type sensorMessageType = GetSensorMessageType(sensorData.SensorType);
                MessageDescriptor sensorMessageDescriptor = null;
                Dictionary<string, FieldDescriptor> fieldDescriptorMap = null;

                if (sensorMessageType != null)
                {
                    var descriptorProperty = sensorMessageType.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static);
                    if (descriptorProperty != null)
                    {
                        sensorMessageDescriptor = descriptorProperty.GetValue(null) as MessageDescriptor;

                        // Collect all field descriptors
                        fieldDescriptorMap = new Dictionary<string, FieldDescriptor>();
                        CollectFieldDescriptors(sensorMessageDescriptor, fieldDescriptorMap);
                    }
                }
                else
                {
                    Console.WriteLine($"Sensor message type not found for sensor type '{sensorData.SensorType}'.");
                }

                // Extract all fields dynamically, including nested structures
                parsedData = ExtractAllFields(sensorData, fieldDescriptorMap);

                // Display parsed fields immediately
                Console.WriteLine($"Parsed data for Pacifier {pacifierId} on sensor type '{detectedSensorType}':");
                DisplayParsedFields(parsedData);

                // Add to sensor data list and trigger event
                lock (_dataLock)
                {
                    _sensorDataList.Add(sensorData);
                    SensorDataUpdated?.Invoke(this, EventArgs.Empty);
                }

                return (pacifierId, detectedSensorType, parsedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse sensor data for Pacifier '{pacifierId}': {ex.Message}");
            }

            return (pacifierId, detectedSensorType, parsedData);
        }

        // Retrieve a list of unique pacifier IDs
        public List<string> GetPacifierIds()
        {
            lock (_dataLock)
            {
                return _sensorDataList.Select(data => data.PacifierId).Distinct().ToList();
            }
        }

        // Retrieve a list of unique pacifier names (IDs)
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

        private Type? GetSensorMessageType(string sensorType)
        {
            var assembly = typeof(SensorData).Assembly;
            string messageTypeName = sensorType.ToUpper() + "Data";

            foreach (var type in assembly.GetTypes())
            {
                if (typeof(IMessage).IsAssignableFrom(type) && type.Namespace == "Protos")
                {
                    if (string.Equals(type.Name, messageTypeName, StringComparison.OrdinalIgnoreCase))
                    {
                        return type;
                    }
                }
            }

            // If not found
            return null;
        }

        private void CollectFieldDescriptors(MessageDescriptor messageDescriptor, Dictionary<string, FieldDescriptor> fieldDescriptorMap)
        {
            foreach (var field in messageDescriptor.Fields.InDeclarationOrder())
            {
                if (!fieldDescriptorMap.ContainsKey(field.Name))
                {
                    fieldDescriptorMap[field.Name] = field;
                }

                if (field.FieldType == FieldType.Message)
                {
                    CollectFieldDescriptors(field.MessageType, fieldDescriptorMap);
                }
            }
        }
        private Dictionary<string, object> ExtractAllFields(IMessage message, Dictionary<string, FieldDescriptor> fieldDescriptorMap = null)
        {
            var result = new Dictionary<string, object>();

            foreach (var field in message.Descriptor.Fields.InFieldNumberOrder())
            {
                var fieldValue = field.Accessor.GetValue(message);

                if (fieldValue is IMessage nestedMessage)
                {
                    result[field.Name] = ExtractAllFields(nestedMessage, fieldDescriptorMap); // Recursively handle nested messages
                }
                else if (fieldValue is RepeatedField<IMessage> repeatedMessage)
                {
                    var repeatedFields = new List<Dictionary<string, object>>();
                    foreach (var item in repeatedMessage)
                    {
                        repeatedFields.Add(ExtractAllFields(item, fieldDescriptorMap)); // Recursively extract repeated messages
                    }
                    result[field.Name] = repeatedFields;
                }
                else if (fieldValue is MapField<string, ByteString> dataMap)
                {
                    var decodedDataMap = new Dictionary<string, object>();
                    foreach (var kvp in dataMap)
                    {
                        // Decode the ByteString without field descriptors
                        var decodedValue = DecodeByteStringDynamic(kvp.Value, kvp.Key);
                        decodedDataMap[kvp.Key] = decodedValue;
                    }
                    result[field.Name] = decodedDataMap;
                }
                else
                {
                    result[field.Name] = fieldValue;
                }
            }

            return result;
        }

        private object DecodeByteStringDynamic(ByteString byteString, string key)
        {
            byte[] bytes = byteString.ToByteArray();

            // Attempt to decode based on the length
            try
            {
                if (bytes.Length == sizeof(float))
                {
                    return BitConverter.ToSingle(bytes, 0);
                }
                else if (bytes.Length == sizeof(int))
                {
                    return BitConverter.ToInt32(bytes, 0);
                }
                else if (bytes.Length == sizeof(double))
                {
                    return BitConverter.ToDouble(bytes, 0);
                }
                else if (bytes.Length == sizeof(long))
                {
                    return BitConverter.ToInt64(bytes, 0);
                }
                else
                {
                    // Fallback to Base64
                    return Convert.ToBase64String(bytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decoding '{key}': {ex.Message}");
                return Convert.ToBase64String(bytes);
            }
        }


        public void DisplayParsedFields(Dictionary<string, object> parsedData, int indentLevel = 0)
        {
            string indent = new string(' ', indentLevel * 2);

            foreach (var kvp in parsedData)
            {
                if (kvp.Value is Dictionary<string, object> nestedDict)
                {
                    Console.WriteLine($"{indent}{kvp.Key}:");
                    DisplayParsedFields(nestedDict, indentLevel + 1);
                }
                else if (kvp.Value is List<Dictionary<string, object>> repeatedFields)
                {
                    Console.WriteLine($"{indent}{kvp.Key} (Repeated Message):");
                    foreach (var item in repeatedFields)
                    {
                        DisplayParsedFields(item, indentLevel + 1);
                    }
                }
                else if (kvp.Value is Dictionary<string, object> dataMap)
                {
                    Console.WriteLine($"{indent}{kvp.Key}:");
                    DisplayParsedFields(dataMap, indentLevel + 1); // Displaying nested map
                }
                else
                {
                    Console.WriteLine($"{indent}{kvp.Key}: {kvp.Value}");
                }
            }
        }
    }
}
