using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Google.Protobuf;
using Protos;
using System.Text.Json;
using Google.Protobuf.Collections;

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

        public (string pacifierId, string sensorType, Dictionary<string, object> parsedData) ParseSensorData(string pacifierId, string topic, byte[] data)
        {
            var parsedData = new Dictionary<string, object>();
            string detectedSensorType = topic.Split('/').Last(); // Assuming the last part of the topic specifies sensor type

            try
            {
                string jsonString = Encoding.UTF8.GetString(data);
                if (jsonString.Trim().StartsWith("{"))
                {
                    Console.WriteLine("Detected JSON format data. Parsing as JSON.");

                    using JsonDocument jsonDoc = JsonDocument.Parse(jsonString);
                    var sensorData = new SensorData { PacifierId = pacifierId, SensorType = detectedSensorType };

                    PopulateSensorDataFromJson(sensorData, jsonDoc.RootElement);

                    parsedData = ExtractAllFields(sensorData);
                }
                else
                {
                    Console.WriteLine("Attempting to parse as Protobuf binary format.");
                    var sensorData = SensorData.Parser.ParseFrom(data);
                    sensorData.SensorType = detectedSensorType;

                    parsedData = ExtractAllFields(sensorData);
                }

                return (pacifierId, detectedSensorType, parsedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse sensor data for Pacifier '{pacifierId}': {ex.Message}");
            }

            return (pacifierId, null, parsedData);
        }


        private void PopulateSensorDataFromJson(SensorData sensorData, JsonElement jsonElement)
        {
            var dataMap = new MapField<string, ByteString>();

            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.Name == "sensor_group" && property.Value.ValueKind == JsonValueKind.String)
                {
                    // Explicitly capture sensor_group if it exists in JSON
                    sensorData.SensorGroup = property.Value.GetString();
                }
                else
                {
                    var fieldDescriptor = SensorData.Descriptor.FindFieldByName(property.Name);

                    if (fieldDescriptor != null)
                    {
                        // Handle known fields directly in the SensorData object
                        switch (fieldDescriptor.FieldType)
                        {
                            case Google.Protobuf.Reflection.FieldType.String:
                                fieldDescriptor.Accessor.SetValue(sensorData, property.Value.GetString());
                                break;
                            case Google.Protobuf.Reflection.FieldType.Int32:
                                fieldDescriptor.Accessor.SetValue(sensorData, property.Value.GetInt32());
                                break;
                            case Google.Protobuf.Reflection.FieldType.Float:
                                fieldDescriptor.Accessor.SetValue(sensorData, (float)property.Value.GetDouble());
                                break;
                            case Google.Protobuf.Reflection.FieldType.Bool:
                                fieldDescriptor.Accessor.SetValue(sensorData, property.Value.GetBoolean());
                                break;
                            default:
                                Console.WriteLine($"Unhandled field type '{fieldDescriptor.FieldType}' for '{property.Name}'");
                                break;
                        }
                    }
                    else
                    {
                        // If it’s nested data or scalar data not part of known fields, add to DataMap
                        if (property.Value.ValueKind == JsonValueKind.Object)
                        {
                            Console.WriteLine($"Adding nested data for '{property.Name}' to DataMap.");
                            dataMap[property.Name] = ParseNestedMessage(property.Value);
                        }
                        else
                        {
                            Console.WriteLine($"Adding scalar data for '{property.Name}' to DataMap.");
                            dataMap[property.Name] = ByteString.CopyFromUtf8(property.Value.ToString());
                        }
                    }
                }
            }

            // Add the populated dataMap to the SensorData instance
            sensorData.DataMap.Add(dataMap);
        }




        private ByteString ParseNestedMessage(JsonElement element)
        {
            var nestedData = new Dictionary<string, object>();

            foreach (var property in element.EnumerateObject())
            {
                nestedData[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.GetDouble(),
                    JsonValueKind.Object => ParseNestedMessage(property.Value),  // Recursion for nested objects
                    _ => property.Value.ToString()
                };
            }

            var nestedMessage = DynamicProtobufMessage(nestedData);
            return nestedMessage?.ToByteString() ?? ByteString.Empty;
        }

        private IMessage DynamicProtobufMessage(Dictionary<string, object> data)
        {
            // Placeholder: This method should dynamically create a Protobuf message from `data`
            // using reflection or dynamic mapping based on field names/types
            throw new NotImplementedException("Implement dynamic Protobuf message creation.");
        }

        private ByteString ParseNestedJsonToProtobuf(JsonElement nestedJson)
        {
            var nestedDataDict = new Dictionary<string, object>();

            foreach (var property in nestedJson.EnumerateObject())
            {
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        nestedDataDict[property.Name] = property.Value.GetString();
                        break;
                    case JsonValueKind.Number:
                        nestedDataDict[property.Name] = property.Value.GetDouble();
                        break;
                    case JsonValueKind.Object:
                        nestedDataDict[property.Name] = ParseNestedJsonToProtobuf(property.Value);
                        break;
                    default:
                        nestedDataDict[property.Name] = property.Value.ToString();
                        break;
                }
            }

            var nestedMessage = DynamicCreateProtobufMessage(nestedDataDict);
            return nestedMessage?.ToByteString() ?? ByteString.Empty;
        }
        private IMessage DynamicCreateProtobufMessage(Dictionary<string, object> data)
        {
            // This method dynamically creates a Protobuf message based on the dictionary structure
            // Placeholder implementation - you need to implement dynamic creation of nested messages
            throw new NotImplementedException("Implement dynamic creation of nested Protobuf messages");
        }
        // Helper function to parse JSON into a dictionary
        // Helper function to parse JSON into a dictionary, taking JsonElement as input
        private Dictionary<string, object> ParseJsonToDictionary(JsonElement element)
        {
            var result = new Dictionary<string, object>();

            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.GetDouble(),
                    JsonValueKind.Object => ParseJsonToDictionary(property.Value), // Recursive parsing for nested JSON objects
                    JsonValueKind.Array => property.Value.EnumerateArray().Select(v => v.ToString()).ToList(),
                    _ => property.Value.ToString()
                };
            }

            return result;
        }



        private IMessage DynamicParseMessage(ByteString byteString)
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IMessage).IsAssignableFrom(t) && t != typeof(SensorData)))
            {
                var parserProperty = type.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
                if (parserProperty != null)
                {
                    var parser = parserProperty.GetValue(null) as MessageParser;
                    try
                    {
                        return parser.ParseFrom(byteString);
                    }
                    catch
                    {
                        // Continue to the next type if parsing fails
                    }
                }
            }

            throw new InvalidOperationException("Unable to parse message, no matching type found.");
        }

        /// <summary>
        /// Recursively extracts all fields from a Protobuf message, including nested and repeated fields.
        /// </summary>
        private Dictionary<string, object> ExtractAllFields(IMessage message)
        {
            var result = new Dictionary<string, object>();

            foreach (var field in message.Descriptor.Fields.InFieldNumberOrder())
            {
                var fieldValue = field.Accessor.GetValue(message);

                if (fieldValue is IMessage nestedMessage)
                {
                    result[field.Name] = ExtractAllFields(nestedMessage);
                }
                else if (fieldValue is RepeatedField<IMessage> repeatedMessage)
                {
                    var repeatedFields = new List<Dictionary<string, object>>();
                    foreach (var item in repeatedMessage)
                    {
                        repeatedFields.Add(ExtractAllFields(item));
                    }
                    result[field.Name] = repeatedFields;
                }
                else if (fieldValue is RepeatedField<float> repeatedFloat)
                {
                    result[field.Name] = repeatedFloat.ToList();
                }
                else if (fieldValue is RepeatedField<int> repeatedInt)
                {
                    result[field.Name] = repeatedInt.ToList();
                }
                else
                {
                    result[field.Name] = fieldValue;
                }
            }

            return result;
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
                else
                {
                    // Decode Base64 values for easier readability
                    if (kvp.Value is string base64String && IsBase64String(base64String))
                    {
                        var decodedValue = Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
                        Console.WriteLine($"{indent}{kvp.Key}: {decodedValue}");
                    }
                    else
                    {
                        Console.WriteLine($"{indent}{kvp.Key}: {kvp.Value}");
                    }
                }
            }
        }

        // Helper function to check if a string is Base64 encoded
        private bool IsBase64String(string value)
        {
            Span<byte> buffer = new Span<byte>(new byte[value.Length]);
            return Convert.TryFromBase64String(value, buffer, out _);
        }

        public List<string> GetPacifierIds()
        {
            lock (_dataLock)
            {
                return _sensorDataList.Select(data => data.PacifierId).Distinct().ToList();
            }
        }

        public List<SensorData> GetAllSensorData()
        {
            lock (_dataLock)
            {
                return _sensorDataList.Select(data => data.Clone()).ToList();
            }
        }

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
