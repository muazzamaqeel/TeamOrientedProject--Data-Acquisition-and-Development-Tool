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
                        FieldDescriptor dataFieldDescriptor = null;
                        if (fieldDescriptorMap != null && fieldDescriptorMap.TryGetValue(kvp.Key, out dataFieldDescriptor))
                        {
                            decodedDataMap[kvp.Key] = DecodeByteStringDynamic(kvp.Value, dataFieldDescriptor);
                        }
                        else
                        {
                            Console.WriteLine($"Field descriptor not found for key '{kvp.Key}'. Defaulting to Base64.");
                            decodedDataMap[kvp.Key] = Convert.ToBase64String(kvp.Value.ToByteArray());
                        }
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

        private object DecodeByteStringDynamic(ByteString byteString, FieldDescriptor fieldDescriptor)
        {
            byte[] bytes = byteString.ToByteArray();

            // Attempt to decode based on the FieldType dynamically
            try
            {
                switch (fieldDescriptor.FieldType)
                {
                    case FieldType.Float:
                        if (bytes.Length == sizeof(float)) return BitConverter.ToSingle(bytes, 0);
                        break;
                    case FieldType.Double:
                        if (bytes.Length == sizeof(double)) return BitConverter.ToDouble(bytes, 0);
                        break;
                    case FieldType.Int32:
                        if (bytes.Length == sizeof(int)) return BitConverter.ToInt32(bytes, 0);
                        break;
                    case FieldType.Int64:
                        if (bytes.Length == sizeof(long)) return BitConverter.ToInt64(bytes, 0);
                        break;
                    case FieldType.UInt32:
                        if (bytes.Length == sizeof(uint)) return BitConverter.ToUInt32(bytes, 0);
                        break;
                    case FieldType.UInt64:
                        if (bytes.Length == sizeof(ulong)) return BitConverter.ToUInt64(bytes, 0);
                        break;
                    case FieldType.String:
                        return System.Text.Encoding.UTF8.GetString(bytes);
                    case FieldType.Message:
                        // For nested messages, use reflection to parse the message type
                        var parserProperty = fieldDescriptor.MessageType.ClrType.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
                        if (parserProperty != null)
                        {
                            var parser = parserProperty.GetValue(null) as MessageParser;
                            if (parser != null)
                            {
                                var nestedMessage = parser.ParseFrom(bytes);
                                return ExtractAllFields(nestedMessage);
                            }
                        }
                        break;
                    default:
                        Console.WriteLine($"Unhandled field type: {fieldDescriptor.FieldType} for field '{fieldDescriptor.Name}'");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decoding '{fieldDescriptor.Name}' as {fieldDescriptor.FieldType}: {ex.Message}");
            }

            // Fallback: return Base64 if unable to decode as recognized type
            Console.WriteLine($"Unable to decode '{fieldDescriptor.Name}' as a recognized type. Returning as Base64.");
            return Convert.ToBase64String(bytes);
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
