using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using SmartPacifier.BackEnd.CommunicationLayer.Protobuf;
using SmartPacifier.Interface.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace SmartPacifier.BackEnd.Database.InfluxDB.Managers
{
    public class ManagerPacifiers : IManagerPacifiers
    {
        private readonly InfluxDBClient _client;
        private readonly IDatabaseService _databaseService;
        private readonly string _bucket = "SmartPacifier-Bucket1";
        private readonly string _org = "thu-de";

        public ManagerPacifiers(IDatabaseService databaseService, InfluxDBClient client)
        {
            _databaseService = databaseService;
            _client = client; // Inject the client from DI
        }





        public List<string> GetPacifierNamesFromSensorData()
        {
            var names = ExposeSensorDataManager.Instance.GetPacifierNames();
            MessageBox.Show($"Retrieved Pacifier Names: {string.Join(", ", names)}");
            return names;
        }





        // ManagerPacifiers-specific method to add a pacifier
        public async Task AddPacifierAsync(string pacifierName)
        {
            var tags = new Dictionary<string, string> { { "pacifier_name", pacifierName } };
            var fields = new Dictionary<string, object>
            {
                { "campaign_name", "Campaign 5" },
                { "sensors", "null" },
                { "status", "active" }
            };

            await WritePacifierDataAsync("pacifiers", fields, tags, DateTime.UtcNow);
        }

        private async Task WritePacifierDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags, DateTime timestamp)
        {
            try
            {
                var point = PointData.Measurement(measurement)
                    .Timestamp(timestamp, WritePrecision.Ns);

                // Add tags
                foreach (var tag in tags)
                {
                    point = point.Tag(tag.Key, tag.Value);
                }

                // Add fields
                foreach (var field in fields)
                {
                    switch (field.Value)
                    {
                        case float floatValue:
                            point = point.Field(field.Key, floatValue);
                            break;
                        case double doubleValue:
                            point = point.Field(field.Key, doubleValue);
                            break;
                        case int intValue:
                            point = point.Field(field.Key, intValue);
                            break;
                        case string stringValue:
                            point = point.Field(field.Key, stringValue);
                            break;
                        default:
                            Console.WriteLine($"Unsupported field type for {field.Key}: {field.Value.GetType()}");
                            break;
                    }
                }

                // Write data to InfluxDB
                var writeApi = _client.GetWriteApiAsync();
                await writeApi.WritePointAsync(point, _bucket, _org);

                Console.WriteLine($"Pacifier '{tags["pacifier_name"]}' added successfully with initial status.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing pacifier data: {ex.Message}");
            }
        }





        // ManagerPacifiers-specific method to get pacifiers by campaign
        public async Task<List<string>> GetPacifiersAsync(string campaignName)
        {
            var pacifiers = new List<string>();
            try
            {
                // Query for pacifiers based on the selected campaign
                var fluxQuery = $"from(bucket: \"{_bucket}\") " +
                                $"|> range(start: -30d) " +
                                $"|> filter(fn: (r) => r[\"_measurement\"] == \"pacifiers\" and r[\"campaign_name\"] == \"{campaignName}\") " +
                                $"|> keep(columns: [\"pacifier_name\"]) " +
                                $"|> distinct(column: \"pacifier_name\")";

                var queryApi = _client.GetQueryApi();
                var tables = await queryApi.QueryAsync(fluxQuery, _org);

                foreach (var table in tables)
                {
                    foreach (var record in table.Records)
                    {
                        var pacifierName = record.GetValueByKey("pacifier_name")?.ToString();
                        if (!string.IsNullOrEmpty(pacifierName))
                        {
                            pacifiers.Add(pacifierName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving pacifiers: {ex.Message}");
            }

            return pacifiers;
        }


        // Implementation of the GetCampaignsAsync method
        public async Task<List<string>> GetCampaignsAsync()
        {
            var campaigns = new List<string>();
            try
            {
                var fluxQuery = $"from(bucket: \"{_bucket}\") |> range(start: -30d) |> keep(columns: [\"campaign_name\"]) |> distinct(column: \"campaign_name\")";
                var queryApi = _client.GetQueryApi();
                var tables = await queryApi.QueryAsync(fluxQuery, _org);

                foreach (var table in tables)
                {
                    foreach (var record in table.Records)
                    {
                        var campaignName = record.GetValueByKey("campaign_name")?.ToString();
                        if (!string.IsNullOrEmpty(campaignName))
                        {
                            campaigns.Add(campaignName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving campaigns: {ex.Message}");
            }

            return campaigns;
        }

        // ReadData method
        public async Task<List<string>> ReadData(string query)
        {
            var queryApi = _client.GetQueryApi();
            var tables = await queryApi.QueryAsync(query, _org);
            var records = new List<string>();

            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    records.Add(record.GetValue().ToString());
                }
            }

            return records;
        }

        // WriteDataAsync method
        public async Task WriteDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags)
        {
            try
            {
                var point = PointData.Measurement(measurement)
                    .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

                // Add tags to the point (Key-Value pairs)
                foreach (var tag in tags)
                {
                    point = point.Tag(tag.Key, tag.Value);
                }

                // Add fields to the point (Key-Value pairs)
                foreach (var field in fields)
                {
                    if (field.Value is float)
                        point = point.Field(field.Key, (float)field.Value);
                    else if (field.Value is double)
                        point = point.Field(field.Key, (double)field.Value);
                    else if (field.Value is int)
                        point = point.Field(field.Key, (int)field.Value);
                    else if (field.Value is string)
                        point = point.Field(field.Key, (string)field.Value);
                }

                var writeApi = _client.GetWriteApiAsync();
                await writeApi.WritePointAsync(point, _bucket, _org);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing data: {ex.Message}");
            }
        }
















        /// <summary>
        /// Need More Information before implementing this method!!!!
        /// </summary>
        /// <param name="pacifierName"></param>
        /// <param name="campaignName"></param>
        /// <param name="sensorType"></param>
        /// <returns></returns>


        public async Task LinkPacifierToCampaignAsync(string pacifierName, string campaignName, string sensorType)
            {
                var tags = new Dictionary<string, string>
                    {
                        { "pacifier_name", pacifierName }
                    };

                                var fields = new Dictionary<string, object>
                    {
                        { "campaign_name", campaignName },
                        { "sensors", sensorType },
                        { "status", "active" } // Keep status active
                    };

                await WritePacifierDataAsync("pacifiers", fields, tags, DateTime.UtcNow);
            }




        }
}

