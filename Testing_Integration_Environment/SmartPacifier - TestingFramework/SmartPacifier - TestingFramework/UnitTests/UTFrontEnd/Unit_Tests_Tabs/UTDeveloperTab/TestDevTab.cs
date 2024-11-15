using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using InfluxDB.Client;
using Newtonsoft.Json;
using SmartPacifier.BackEnd.Database.InfluxDB.Connection;

namespace SmartPacifier___TestingFramework.UnitTests.UTFrontEnd.Unit_Tests_Tabs.UTDeveloperTab
{
    public class TestDevTab
    {
        private readonly InfluxDatabaseService _databaseService;

        public TestDevTab()
        {
            // Load configuration from JSON
            var config = LoadDatabaseConfiguration();

            // Use configuration values to initialize InfluxDBClient
            string host = config.UseLocal ? config.Local.Host : $"{config.Server.Host}:{config.Server.Port}";
            string apiKey = config.UseLocal ? config.Local.ApiKey : config.Server.ApiKey;

            // Ensure the host has the correct URI format
            if (!host.StartsWith("http://") && !host.StartsWith("https://"))
            {
                host = "http://" + host;
            }

            var influxClient = new InfluxDBClient(host, apiKey);

            _databaseService = new InfluxDatabaseService(influxClient, apiKey, host, "thu-de");
        }


        [Fact]
        public async Task DeleteEntryFromDatabaseAsync_ShouldDeleteEntrySuccessfully()
        {
            var testEntryId = 99;
            var measurement = "campaign_metadata";
            var fields = new Dictionary<string, object>
            {
                { "status", "created" },
                { "entry_time", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
            };
            var tags = new Dictionary<string, string>
            {
                { "campaign_name", "Campaign_Test" },
                { "entry_id", testEntryId.ToString() }
            };

            await _databaseService.WriteDataAsync(measurement, fields, tags);

            // Delete the entry
            await _databaseService.DeleteEntryFromDatabaseAsync(testEntryId, measurement);

            // Verify deletion
            string query = $"from(bucket: \"{_databaseService.Bucket}\") |> range(start: -1h) |> filter(fn: (r) => r._measurement == \"{measurement}\" and r.entry_id == \"{testEntryId}\")";
            var result = await _databaseService.ReadData(query);

            Assert.Empty(result); // Should be empty if deletion was successful
        }

        private AppConfiguration LoadDatabaseConfiguration()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "OutputResources", "config.json");

            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException("Configuration file not found.", configPath);
            }

            string configJson = File.ReadAllText(configPath);
            return JsonConvert.DeserializeObject<AppConfiguration>(configJson);
        }
    }

    public class AppConfiguration
    {
        public bool UseLocal { get; set; }
        public LocalConfig Local { get; set; }
        public ServerConfig Server { get; set; }
    }

    public class LocalConfig
    {
        public string Host { get; set; }
        public string ApiKey { get; set; }
    }

    public class ServerConfig
    {
        public string Host { get; set; }
        public string Port { get; set; }
        public string ApiKey { get; set; }
    }
}
