using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using SmartPacifier.Interface.Services;

namespace SmartPacifier.BackEnd.Database.InfluxDB.Connection
{
    public class InfluxDatabaseService : IDatabaseService
    {
        private readonly InfluxDBClient _client;
        private readonly string _bucket = "SmartPacifier-Bucket1";
        private readonly string _org = "thu-de";

        // Inject InfluxDBClient instead of URL and Token
        public InfluxDatabaseService(InfluxDBClient client)
        {
            _client = client;
        }

        public InfluxDBClient GetClient() => _client;

        public async Task WriteDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags)
        {
            try
            {
                var point = PointData.Measurement(measurement)
                    .Timestamp(DateTime.UtcNow, WritePrecision.Ns); // Add timestamp first

                // Add tags to the point (Key-Value pairs)
                foreach (var tag in tags)
                {
                    point = point.Tag(tag.Key, tag.Value); // Correctly pass both key and value
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

        public async Task<List<string>> ReadData(string query)
        {
            var queryApi = _client.GetQueryApi();
            var tables = await queryApi.QueryAsync(query, _org);
            var records = new List<string>();

            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    var value = record.GetValue();
                    if (value != null)  // Check for null
                    {
                        records.Add(value.ToString());
                    }
                }
            }

            return records;
        }


        public async Task<List<string>> GetCampaignsAsync()
        {
            var campaigns = new List<string>();
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

            return campaigns;
        }



    }
}
