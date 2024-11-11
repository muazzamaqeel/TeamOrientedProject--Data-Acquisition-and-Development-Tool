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
                //Console.WriteLine($"Received raw data length: {data.Length} for Pacifier ID: {pacifierId}");
                //Console.WriteLine($"Raw Data (Hex): {BitConverter.ToString(data)}");

                // Attempt to parse data as JSON first
                string jsonString = Encoding.UTF8.GetString(data);
                if (jsonString.Trim().StartsWith("{"))
                {
                    Console.WriteLine("Detected JSON format data. Parsing as JSON.");
                    using JsonDocument jsonDoc = JsonDocument.Parse(jsonString);
                    parsedData = ParseJsonToDictionary(jsonDoc.RootElement);
                }
                else
                {
                    // Otherwise, attempt to parse as Protobuf
                    Console.WriteLine("Attempting to parse as Protobuf binary format.");
                    var sensorData = SensorData.Parser.ParseFrom(data);

                    // If parsed as Protobuf, populate parsedData from sensorData
                    parsedData = ExtractAllFields(sensorData);
                }

                return (pacifierId, "DetectedFormat", parsedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse sensor data for Pacifier '{pacifierId}': {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            return (null, null, null);
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
