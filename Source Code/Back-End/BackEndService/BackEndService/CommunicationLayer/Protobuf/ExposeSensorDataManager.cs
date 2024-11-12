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

        public (string pacifierId, string sensorType, Dictionary<string, object> parsedData) ParseSensorData(string pacifierId, byte[] data)
        {
            var parsedData = new Dictionary<string, object>();
            try
            {
                // Attempt to parse data as JSON
                string jsonString = Encoding.UTF8.GetString(data);
                if (jsonString.Trim().StartsWith("{"))
                {
                    Console.WriteLine("Detected JSON format data. Parsing as JSON.");

                    // Use JsonDocument to parse JSON and dynamically populate SensorData using reflection
                    using JsonDocument jsonDoc = JsonDocument.Parse(jsonString);
                    var sensorData = new SensorData();
                    PopulateSensorDataFromJson(sensorData, jsonDoc.RootElement);

                    // Debugging to inspect each field after setting values using reflection
                    Console.WriteLine($"pacifier_id: {sensorData.PacifierId}");
                    Console.WriteLine($"sensor_type: {sensorData.SensorType}");
                    Console.WriteLine($"sensor_group: {sensorData.SensorGroup}");

                    if (sensorData.DataMap != null && sensorData.DataMap.Count > 0)
                    {
                        foreach (var kvp in sensorData.DataMap)
                        {
                            Console.WriteLine($"DataMap Key: {kvp.Key}, Value: {Encoding.UTF8.GetString(kvp.Value.ToByteArray())}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("DataMap is empty or null.");
                    }

                    parsedData = ExtractAllFields(sensorData);
                }
                else
                {
                    // If not JSON, parse as Protobuf binary
                    Console.WriteLine("Attempting to parse as Protobuf binary format.");
                    var sensorData = SensorData.Parser.ParseFrom(data);

                    parsedData = ExtractAllFields(sensorData);
                }

                return (pacifierId, "DetectedFormat", parsedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse sensor data for Pacifier '{pacifierId}': {ex.Message}");
            }

            return (pacifierId, null, parsedData);
        }


        private void PopulateSensorDataFromJson(SensorData sensorData, JsonElement jsonElement)
        {
            foreach (var property in jsonElement.EnumerateObject())
            {
                var fieldDescriptor = SensorData.Descriptor.FindFieldByName(property.Name);

                if (fieldDescriptor != null)
                {
                    switch (fieldDescriptor.FieldType)
                    {
                        case Google.Protobuf.Reflection.FieldType.String:
                            fieldDescriptor.Accessor.SetValue(sensorData, property.Value.GetString());
                            break;
                        case Google.Protobuf.Reflection.FieldType.Message:
                            if (property.Name == "data_map" && property.Value.ValueKind == JsonValueKind.Object)
                            {
                                ParseNestedDataIntoDataMap(sensorData, property.Value);
                            }
                            break;
                        default:
                            Console.WriteLine($"Field type '{fieldDescriptor.FieldType}' for '{property.Name}' not handled.");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine($"Field '{property.Name}' not found in SensorData.");
                }
            }
        }

        private void ParseNestedDataIntoDataMap(SensorData sensorData, JsonElement dataMapElement)
        {
            var dataMap = new MapField<string, ByteString>();

            foreach (var kvp in dataMapElement.EnumerateObject())
            {
                if (kvp.Name.StartsWith("imu"))
                {
                    var imuData = new IMUData();
                    PopulateIMUData(imuData, kvp.Value);
                    dataMap[kvp.Name] = imuData.ToByteString();
                }
                else if (kvp.Name.StartsWith("ppg"))
                {
                    var ppgData = new PPGData();
                    PopulatePPGData(ppgData, kvp.Value);
                    dataMap[kvp.Name] = ppgData.ToByteString();
                }
            }

            sensorData.DataMap.Add(dataMap);
        }

        private void PopulateIMUData(IMUData imuData, JsonElement imuElement)
        {
            foreach (var property in imuElement.EnumerateObject())
            {
                foreach (var field in IMUData.Descriptor.Fields.InFieldNumberOrder())
                {
                    if (field.Name == property.Name)
                    {
                        switch (field.FieldType)
                        {
                            case Google.Protobuf.Reflection.FieldType.Float:
                                imuData.GetType().GetProperty(field.Name)?.SetValue(imuData, (float)property.Value.GetDouble());
                                break;
                            default:
                                Console.WriteLine($"IMU field type '{field.FieldType}' for '{property.Name}' not handled.");
                                break;
                        }
                    }
                }
            }
        }

        private void PopulatePPGData(PPGData ppgData, JsonElement ppgElement)
        {
            foreach (var property in ppgElement.EnumerateObject())
            {
                foreach (var field in PPGData.Descriptor.Fields.InFieldNumberOrder())
                {
                    if (field.Name == property.Name)
                    {
                        switch (field.FieldType)
                        {
                            case Google.Protobuf.Reflection.FieldType.Int32:
                                ppgData.GetType().GetProperty(field.Name)?.SetValue(ppgData, property.Value.GetInt32());
                                break;
                            case Google.Protobuf.Reflection.FieldType.Float:
                                ppgData.GetType().GetProperty(field.Name)?.SetValue(ppgData, (float)property.Value.GetDouble());
                                break;
                            default:
                                Console.WriteLine($"PPG field type '{field.FieldType}' for '{property.Name}' not handled.");
                                break;
                        }
                    }
                }
            }
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
                    Console.WriteLine($"{indent}{kvp.Key}: {kvp.Value}");
                }
            }
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
