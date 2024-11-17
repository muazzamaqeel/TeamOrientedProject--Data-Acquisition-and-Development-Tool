using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using Protos;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;
using InfluxData.Net.InfluxDb.Models.Responses;
using System.Collections.ObjectModel;

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

        public (string pacifierId, string sensorType, ObservableCollection<Dictionary<string, object>> parsedData) ParseSensorData(byte[] data)
        {
            var parsedData = new ObservableCollection<Dictionary<string, object>>();
            var pacifierId = "defaultPacifier";
            var sensorType = "defaultSensor";

            try
            {
                var sensorData = SensorData.Parser.ParseFrom(data);
                pacifierId = sensorData.PacifierId;
                sensorType = sensorData.SensorType;

                // Get the message type corresponding to sensorData.SensorType
                Type sensorMessageType = GetSensorMessageType(sensorData.SensorType);
                MessageDescriptor sensorMessageDescriptor = null;
                Dictionary<string, FieldDescriptor> fieldDescriptorMap = null;

                MapField<string, ByteString> sensorDataDictionary = sensorData.DataMap;


                if (sensorMessageType != null)
                {
                    var descriptorProperty = sensorMessageType.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static);
                    if (descriptorProperty != null)
                    {
                        sensorMessageDescriptor = descriptorProperty.GetValue(null) as MessageDescriptor;

                        // Collect all field descriptors
                        fieldDescriptorMap = new Dictionary<string, FieldDescriptor>();
                        parsedData = CollectFieldDescriptors(sensorMessageDescriptor, fieldDescriptorMap, sensorDataDictionary);
                    }
                }
                else
                {
                    Console.WriteLine($"Sensor message type not found for sensor type '{sensorType}'.");
                }

                // Extract all fields dynamically, including nested structures
                //parsedData = ExtractAllFields(sensorData, fieldDescriptorMap);

                // Display parsed fields immediately
                Console.WriteLine($"Parsed data for Pacifier {pacifierId} on sensor type '{sensorType}':");
                //DisplayParsedFields(parsedData);

                // Add to sensor data list and trigger event
                lock (_dataLock)
                {
                    _sensorDataList.Add(sensorData);
                    
                }
                SensorDataUpdated?.Invoke(this, EventArgs.Empty);

                return (pacifierId, sensorType, parsedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse sensor data for Pacifier '{pacifierId}': {ex.Message}");
            }

            return (pacifierId, sensorType, parsedData);
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

        private ObservableCollection<Dictionary<string, object>> CollectFieldDescriptors(MessageDescriptor messageDescriptor, Dictionary<string, FieldDescriptor> fieldDescriptorMap, MapField<string, ByteString> sensorDataDictionary)
        {
            var result = new ObservableCollection<Dictionary<string, object>>();  // List of dictionaries to hold the final result
            var groupDictionary = new Dictionary<string, object>();  // Temporary dictionary to hold each field and its value

            // Iterate through all fields in the message descriptor
            foreach (var field in messageDescriptor.Fields.InDeclarationOrder())
            {
                // Check if the field has not been processed yet
                if (!fieldDescriptorMap.ContainsKey(field.Name))
                {
                    // Check if the field is not a message and if the sensor data dictionary contains the field
                    if (field.FieldType != FieldType.Message && sensorDataDictionary.TryGetValue(field.Name, out ByteString? value))
                    {
                        // Get the ByteString value from the dictionary
                        ByteString fieldValue = value;

                        // Decode the field value (you would decode the byte data based on its field type)
                        var decodedValue = DecodeByteStringDynamic(fieldValue, field);

                        // Add the field and its decoded value to the temporary dictionary
                        groupDictionary[field.Name] = decodedValue;
                    }

                    // Mark this field as processed by adding it to the field descriptor map
                    fieldDescriptorMap[field.Name] = field;
                }

                // Handle nested fields (messages within messages)
                if (field.FieldType == FieldType.Message)
                {
                    // Create a new dictionary for the nested field and add it to the result
                    var nestedDictionary = new Dictionary<string, object>();
                    nestedDictionary["sensorGroup"] = field.Name;  // Add sensor group info

                    // Recursively collect data for the nested message
                    var nestedResult = CollectFieldDescriptors(field.MessageType, fieldDescriptorMap, sensorDataDictionary);

                    // Merge the nested fields into the nested dictionary
                    foreach (var nestedItem in nestedResult)
                    {
                        foreach (var kvp in nestedItem)
                        {
                            nestedDictionary[kvp.Key] = kvp.Value;
                        }
                    }

                    // Add the nested dictionary to the final result
                    result.Add(nestedDictionary);
                }
            }

            // If there are any fields added to the group dictionary, add the group to the result list
            if (groupDictionary.Count > 0)
            {
                result.Add(groupDictionary);
            }

            return result;  // Return the final list of dictionaries
        }



        private object DecodeByteStringDynamic(ByteString byteString, FieldDescriptor fieldDescriptor)
        {
            byte[] bytes = byteString.ToByteArray();

            try
            {
                // Decode based on field type using the FieldDescriptor
                switch (fieldDescriptor.FieldType)
                {
                    case FieldType.Int32:
                        return BitConverter.ToInt32(bytes, 0);
                    case FieldType.Int64:
                        return BitConverter.ToInt64(bytes, 0);
                    case FieldType.UInt32:
                        return BitConverter.ToUInt32(bytes, 0);
                    case FieldType.UInt64:
                        return BitConverter.ToUInt64(bytes, 0);
                    case FieldType.SInt32:
                        return BitConverter.ToInt32(bytes, 0);
                    case FieldType.SInt64:
                        return BitConverter.ToInt64(bytes, 0);
                    case FieldType.Float:
                        return BitConverter.ToSingle(bytes, 0);
                    case FieldType.Double:
                        return BitConverter.ToDouble(bytes, 0);
                    case FieldType.Bool:
                        return BitConverter.ToBoolean(bytes, 0);
                    case FieldType.String:
                        return System.Text.Encoding.UTF8.GetString(bytes);
                    case FieldType.Bytes:
                        return bytes; // Return raw byte array
                    case FieldType.Enum:
                        var enumType = fieldDescriptor.EnumType;
                        return Enum.ToObject(enumType.ClrType, BitConverter.ToInt32(bytes, 0));
                    case FieldType.Message:
                        // If it's a nested message, try to decode it accordingly
                        var nestedMessageType = fieldDescriptor.MessageType;
                        var message = Activator.CreateInstance(nestedMessageType.ClrType) as IMessage;
                        if (message != null)
                        {
                            message.MergeFrom(bytes);
                            return message;
                        }
                        break;
                    default:
                        return Convert.ToBase64String(bytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decoding field: {ex.Message}");
            }

            return Convert.ToBase64String(bytes); // Fallback to Base64 if no decoding is successful
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
