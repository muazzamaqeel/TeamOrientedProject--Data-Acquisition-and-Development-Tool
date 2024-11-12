using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Protos;
using SmartPacifier.Interface.Services;
using System.Data;
using System.Globalization;
using InfluxDB.Client.Core.Exceptions;

namespace SmartPacifier.BackEnd.Database.InfluxDB.Connection
{
    public class InfluxDatabaseService : IDatabaseService
    {
        private readonly InfluxDBClient _client;
        private readonly string _bucket = "SmartPacifier-Bucket1";
        private readonly string _org = "thu-de";
        private readonly string _token;
        private readonly string _baseUrl;
        public InfluxDatabaseService(InfluxDBClient client, string token, string baseUrl, string org)
        {
            _client = client;
            _token = token;
            _baseUrl = baseUrl;
            _org = org;

        }

        public InfluxDBClient GetClient() => _client;

        public string Bucket => _bucket;
        public string Org => _org;
        public string Token => _token; // Implement Token
        public string BaseUrl => _baseUrl; // Implement BaseUrl

        public async Task WriteDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags)
        {
            try
            {
                var point = PointData.Measurement(measurement)
                    .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

                foreach (var tag in tags)
                {
                    point = point.Tag(tag.Key, tag.Value);
                }

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
            catch (UnauthorizedException)
            {
                MessageBox.Show("Unauthorized access to InfluxDB. Please check the API key and permissions.", "Authorization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error writing data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
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
                    var recordData = new Dictionary<string, object>();

                    foreach (var key in record.Values.Keys)
                    {
                        recordData[key] = record.GetValueByKey(key);
                    }

                    // Convert the record dictionary to JSON
                    var json = System.Text.Json.JsonSerializer.Serialize(recordData);
                    records.Add(json);
                }
            }

            // Optional: Show message box to verify the JSON records
            System.Windows.MessageBox.Show(string.Join("\n\n", records), "Raw JSON Data", MessageBoxButton.OK, MessageBoxImage.Information);

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



        public async Task<DataTable> GetSensorDataAsync()
        {
            string fluxQuery = @"
                    from(bucket: ""SmartPacifier-Bucket1"")
                    |> range(start: 0)
                    |> filter(fn: (r) => r._measurement == ""campaigns"")
                    |> pivot(rowKey: [""_time""], columnKey: [""_field""], valueColumn: ""_value"")
                    ";

            DataTable dataTable = new DataTable();

            // Define all columns explicitly to match XAML
            string[] columnNames = { "Measurement", "Campaign Name", "Pacifier Name", "Sensor Type", "Status", "LED1", "LED2", "LED3",
                             "Temperature", "Acc X", "Acc Y", "Acc Z", "Gyro X", "Gyro Y", "Gyro Z", "Mag X", "Mag Y", "Mag Z",
                             "Creation", "Start Time", "End Time", "Timestamp" };

            foreach (string colName in columnNames)
            {
                if (!dataTable.Columns.Contains(colName))
                {
                    dataTable.Columns.Add(colName);
                }
            }

            var queryApi = _client.GetQueryApi();
            var tables = await queryApi.QueryAsync(fluxQuery, _org);

            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    DataRow row = dataTable.NewRow();

                    row["Timestamp"] = record.GetTime()?.ToDateTimeUtc().ToString("o");
                    row["Measurement"] = record.GetValueByKey("_measurement")?.ToString();
                    row["Campaign Name"] = record.GetValueByKey("campaign_name")?.ToString();
                    row["Pacifier Name"] = record.GetValueByKey("pacifier_name")?.ToString();
                    row["Sensor Type"] = record.GetValueByKey("sensor_type")?.ToString();
                    row["Status"] = record.GetValueByKey("status")?.ToString();
                    row["LED1"] = record.GetValueByKey("led1") ?? DBNull.Value;
                    row["LED2"] = record.GetValueByKey("led2") ?? DBNull.Value;
                    row["LED3"] = record.GetValueByKey("led3") ?? DBNull.Value;
                    row["Temperature"] = record.GetValueByKey("temperature") ?? DBNull.Value;
                    row["Acc X"] = record.GetValueByKey("acc_x") ?? DBNull.Value;
                    row["Acc Y"] = record.GetValueByKey("acc_y") ?? DBNull.Value;
                    row["Acc Z"] = record.GetValueByKey("acc_z") ?? DBNull.Value;
                    row["Gyro X"] = record.GetValueByKey("gyro_x") ?? DBNull.Value;
                    row["Gyro Y"] = record.GetValueByKey("gyro_y") ?? DBNull.Value;
                    row["Gyro Z"] = record.GetValueByKey("gyro_z") ?? DBNull.Value;
                    row["Mag X"] = record.GetValueByKey("mag_x") ?? DBNull.Value;
                    row["Mag Y"] = record.GetValueByKey("mag_y") ?? DBNull.Value;
                    row["Mag Z"] = record.GetValueByKey("mag_z") ?? DBNull.Value;
                    row["Creation"] = record.GetValueByKey("creation")?.ToString();
                    row["Start Time"] = record.GetValueByKey("start_time")?.ToString();
                    row["End Time"] = record.GetValueByKey("end_time")?.ToString();

                    dataTable.Rows.Add(row);
                }
            }

            return dataTable;
        }






    }
}
