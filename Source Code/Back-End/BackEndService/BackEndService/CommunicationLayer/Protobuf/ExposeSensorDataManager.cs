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

                // Extract all fields dynamically, including nested structures
                parsedData = ExtractAllFields(sensorData);

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

        private Dictionary<string, object> ExtractAllFields(IMessage message)
        {
            var result = new Dictionary<string, object>();
            Console.WriteLine($"Extracting fields from message: {message.Descriptor.Name}");

            foreach (var field in message.Descriptor.Fields.InFieldNumberOrder())
            {
                var fieldValue = field.Accessor.GetValue(message);
                Console.WriteLine($"Processing field '{field.Name}' with value '{fieldValue}' of type '{fieldValue?.GetType()}'");

                if (fieldValue is IMessage nestedMessage)
                {
                    // Recursively extract fields from nested messages
                    result[field.Name] = ExtractAllFields(nestedMessage);
                }
                else if (fieldValue is RepeatedField<IMessage> repeatedMessage)
                {
                    var repeatedFields = new List<Dictionary<string, object>>();
                    foreach (var item in repeatedMessage)
                    {
                        Console.WriteLine($"Processing repeated message field '{field.Name}' with nested message type '{item.Descriptor.Name}'");
                        repeatedFields.Add(ExtractAllFields(item));
                    }
                    result[field.Name] = repeatedFields;
                }
                else if (fieldValue is MapField<string, ByteString> dataMap)
                {
                    // Decode each ByteString entry in data_map using reflection for dynamic deserialization
                    var decodedDataMap = new Dictionary<string, object>();
                    foreach (var kvp in dataMap)
                    {
                        Console.WriteLine($"Decoding data_map entry for key '{kvp.Key}' with ByteString value");
                        decodedDataMap[kvp.Key] = DecodeByteStringWithReflection(kvp.Value, kvp.Key, message.Descriptor);
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






        private object DecodeByteStringWithReflection(ByteString byteString, string fieldName, MessageDescriptor parentDescriptor)
        {
            byte[] bytes = byteString.ToByteArray();
            Console.WriteLine($"Attempting to decode ByteString for field '{fieldName}'");

            // Attempt to decode as nested type within PPGData or IMUData
            foreach (var nestedType in parentDescriptor.NestedTypes)
            {
                if (nestedType.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Field '{fieldName}' matches nested type '{nestedType.Name}', attempting to parse as nested message");
                    var parserProperty = nestedType.ClrType.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
                    if (parserProperty != null)
                    {
                        var parser = parserProperty.GetValue(null) as MessageParser;
                        if (parser != null)
                        {
                            try
                            {
                                var nestedMessage = parser.ParseFrom(bytes);
                                Console.WriteLine($"Successfully parsed nested message for field '{fieldName}'");
                                return ExtractAllFields(nestedMessage); // Recursively extract nested message fields
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to parse '{fieldName}' as a nested type: {ex.Message}");
                            }
                        }
                    }
                }
            }

            // Attempt to decode as known primitive types
            try
            {
                if (bytes.Length == sizeof(float))
                {
                    float value = BitConverter.ToSingle(bytes, 0);
                    Console.WriteLine($"Decoded '{fieldName}' as float: {value}");
                    return value;
                }
                if (bytes.Length == sizeof(int))
                {
                    int value = BitConverter.ToInt32(bytes, 0);
                    Console.WriteLine($"Decoded '{fieldName}' as int: {value}");
                    return value;
                }
                if (bytes.Length == sizeof(double))
                {
                    double value = BitConverter.ToDouble(bytes, 0);
                    Console.WriteLine($"Decoded '{fieldName}' as double: {value}");
                    return value;
                }
                if (bytes.Length == sizeof(long))
                {
                    long value = BitConverter.ToInt64(bytes, 0);
                    Console.WriteLine($"Decoded '{fieldName}' as long: {value}");
                    return value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decoding '{fieldName}' as a known type: {ex.Message}");
            }

            // Fallback: Return as Base64 if unable to parse
            Console.WriteLine($"Unable to decode '{fieldName}' as a known type. Returning as Base64.");
            return Convert.ToBase64String(bytes);
        }


        private object DecodeByteString(ByteString byteString, string fieldName, MessageDescriptor parentDescriptor)
        {
            byte[] bytes = byteString.ToByteArray();

            try
            {
                if (bytes.Length == sizeof(float)) return BitConverter.ToSingle(bytes, 0);
                if (bytes.Length == sizeof(int)) return BitConverter.ToInt32(bytes, 0);
                if (bytes.Length == sizeof(double)) return BitConverter.ToDouble(bytes, 0);
                if (bytes.Length == sizeof(long)) return BitConverter.ToInt64(bytes, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decoding '{fieldName}' as a known type: {ex.Message}");
            }

            foreach (var nestedType in parentDescriptor.NestedTypes)
            {
                if (nestedType.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    var parserProperty = nestedType.ClrType.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
                    if (parserProperty != null)
                    {
                        var parser = parserProperty.GetValue(null) as MessageParser;
                        if (parser != null)
                        {
                            try
                            {
                                return parser.ParseFrom(bytes);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to parse '{fieldName}' as a nested type: {ex.Message}");
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Unable to dynamically match '{fieldName}'. Returning as Base64 for review.");
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
