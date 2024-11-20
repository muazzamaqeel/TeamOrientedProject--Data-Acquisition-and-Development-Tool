using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using System.Diagnostics;
using Newtonsoft.Json;
using SmartPacifier.BackEnd.Database.InfluxDB.Connection;
using SmartPacifier___TestingFramework.UnitTests.UTFrontEnd.Unit_Tests_Tabs.UTDeveloperTab;
using InfluxDB.Client.Api.Domain;
using FileDomain = InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
namespace SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTDatabaseLayer.UTInfluxDB
{
    public class TestDB
    {
        private readonly InfluxDatabaseService _databaseService;

        public TestDB()
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

            var influxClient = new InfluxDB.Client.InfluxDBClient(host, apiKey);

            _databaseService = new InfluxDatabaseService(influxClient, apiKey, host, "thu-de");
        }
        private AppConfiguration LoadDatabaseConfiguration()
        {
            // Explicitly qualify System.IO.File to resolve ambiguity
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "OutputResources", "config.json");

            if (!System.IO.File.Exists(configPath)) // Specify System.IO.File explicitly
            {
                throw new FileNotFoundException("Configuration file not found.", configPath);
            }

            string configJson = System.IO.File.ReadAllText(configPath); // Specify System.IO.File explicitly
            return JsonConvert.DeserializeObject<AppConfiguration>(configJson);
        }


        [Fact]
        public async Task CampaignCycleTest()
        {
            // Arrange
            string campaignName = "Campaign_Test";
            string measurement = "campaign_metadata";
            var fields = new Dictionary<string, object>
            {
                { "status", "created" },
                { "entry_time", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
            };
            var tags = new Dictionary<string, string>
            {
                { "campaign_name", campaignName }
            };

            // Add the campaign to the database
            await _databaseService.WriteDataAsync(measurement, fields, tags);

            // Act - Query the database to check if the campaign exists
            string query = $@"
                from(bucket: ""{_databaseService.Bucket}"")
                |> range(start: -1h)
                |> filter(fn: (r) => r._measurement == ""{measurement}"" and r.campaign_name == ""{campaignName}"")";

            var result = await _databaseService.ReadData(query);

            // Assert
            Assert.True(result.Count > 0, "Expected the campaign to exist in the database.");
        }


        [Fact]

        public async Task TestPerformance()
        {
            // Arrange: Define the measurements and sample data
            string measurementMetadata = "campaign_metadata";
            string measurementCampaigns = "campaigns";

            var sampleMetadata = new List<Dictionary<string, object>>
        {
        new Dictionary<string, object>
        {
            { "campaign_name", "Campaign3" },
            { "entry_id", 8 },
            { "status", "created" },
            { "entry_time", "2024-10-02 08:00:00" }
        },
        new Dictionary<string, object>
        {
            { "campaign_name", "Campaign3" },
            { "entry_id", 9 },
            { "status", "started" },
            { "entry_time", "2024-10-02 08:05:00" }
        },
        new Dictionary<string, object>
        {
            { "campaign_name", "Campaign3" },
            { "entry_id", 10 },
            { "status", "stopped" },
            { "entry_time", "2024-10-02 08:30:00" }
                }
        };

            var sampleCampaigns = new List<Dictionary<string, object>>
    {
        new Dictionary<string, object>
        {
            { "campaign_name", "Campaign3" },
            { "pacifier_name", "Pacifier1" },
            { "sensor_type", "ppg" },
            { "entry_id", 11 },
            { "led1", 103 },
            { "led2", 107 },
            { "led3", 105 },
            { "temperature", 37.0 },
            { "entry_time", "2024-10-02 08:35:00" }
        },
        new Dictionary<string, object>
        {
            { "campaign_name", "Campaign3" },
            { "pacifier_name", "Pacifier1" },
            { "sensor_type", "imu" },
            { "entry_id", 12 },
            { "acc_x", 0.05 },
            { "acc_y", -0.06 },
            { "acc_z", 0.02 },
            { "gyro_x", -0.01 },
            { "gyro_y", 0.08 },
            { "gyro_z", -0.02 },
            { "mag_x", 0.04 },
            { "mag_y", 0.02 },
            { "mag_z", -0.03 },
            { "entry_time", "2024-10-02 08:36:00" }
        },
        new Dictionary<string, object>
        {
            { "campaign_name", "Campaign3" },
            { "pacifier_name", "Pacifier3" },
            { "sensor_type", "ppg" },
            { "entry_id", 13 },
            { "led1", 104 },
            { "led2", 106 },
            { "led3", 102 },
            { "temperature", 37.1 },
            { "entry_time", "2024-10-02 08:40:00" }
        }
    };

            var stopwatch = new Stopwatch();

            // Act - Measure Write Performance
            stopwatch.Start();
            foreach (var data in sampleMetadata)
            {
                var fields = new Dictionary<string, object>
            {
                { "status", data["status"] },
                { "entry_time", data["entry_time"] }
            };
                    var tags = new Dictionary<string, string>
            {
                { "campaign_name", data["campaign_name"].ToString() },
                { "entry_id", data["entry_id"].ToString() }
            };

                await _databaseService.WriteDataAsync(measurementMetadata, fields, tags);
            }

            foreach (var sensor in sampleCampaigns)
            {
                var fields = new Dictionary<string, object>(sensor);
                fields.Remove("campaign_name");
                fields.Remove("pacifier_name");
                fields.Remove("sensor_type");
                fields.Remove("entry_id");

                var tags = new Dictionary<string, string>
                {
                    { "campaign_name", sensor["campaign_name"].ToString() },
                    { "pacifier_name", sensor["pacifier_name"].ToString() },
                    { "sensor_type", sensor["sensor_type"].ToString() },
                    { "entry_id", sensor["entry_id"].ToString() }
                };

                await _databaseService.WriteDataAsync(measurementCampaigns, fields, tags);
            }
            stopwatch.Stop();
            var writeDuration = stopwatch.ElapsedMilliseconds;

            // Act - Measure Read Performance
            stopwatch.Restart();
            string queryMetadata = $@"
                from(bucket: ""{_databaseService.Bucket}"")
                |> range(start: -1h)
                |> filter(fn: (r) => r._measurement == ""{measurementMetadata}"")";

                    string queryCampaigns = $@"
                from(bucket: ""{_databaseService.Bucket}"")
                |> range(start: -1h)
                |> filter(fn: (r) => r._measurement == ""{measurementCampaigns}"")";

            var metadataResult = await _databaseService.ReadData(queryMetadata);
            var campaignsResult = await _databaseService.ReadData(queryCampaigns);
            stopwatch.Stop();
            var readDuration = stopwatch.ElapsedMilliseconds;

            // Act - Measure Delete Performance
            stopwatch.Restart();
            foreach (var data in sampleMetadata)
            {
                await DeleteEntryAsync(measurementMetadata, (int)data["entry_id"]);
            }

            foreach (var sensor in sampleCampaigns)
            {
                await DeleteEntryAsync(measurementCampaigns, (int)sensor["entry_id"]);
            }
            stopwatch.Stop();
            var deleteDuration = stopwatch.ElapsedMilliseconds;

            // Assert
            Assert.NotEmpty(metadataResult); // Ensure metadata was read successfully
            Assert.NotEmpty(campaignsResult); // Ensure campaigns data was read successfully
            Assert.True(writeDuration < 5000, $"Writing took too long: {writeDuration}ms");
            Assert.True(readDuration < 3000, $"Reading took too long: {readDuration}ms");
            Assert.True(deleteDuration < 5000, $"Deleting took too long: {deleteDuration}ms");

            // Output performance metrics (Console.WriteLine for non-interactive logging)
            Console.WriteLine($"Write Duration: {writeDuration}ms");
            Console.WriteLine($"Read Duration: {readDuration}ms");
            Console.WriteLine($"Delete Duration: {deleteDuration}ms");
        }


        [Fact]
        public async Task PacifierCycleTest()
        {
            // Arrange
            string campaignName = "Campaign2";
            string pacifierName = "Pacifier1";
            string measurement = "campaigns";
            int entryId = 4; // This is the unique ID for the pacifier entry
            var fields = new Dictionary<string, object>
    {
                { "sensor_type", "ppg" },
                { "led1", 99 },
                { "led2", 102 },
                { "led3", 97 },
                { "temperature", 36.4 },
                { "entry_time", "2024-10-01 12:35:00" }
            };
                    var tags = new Dictionary<string, string>
            {
                { "campaign_name", campaignName },
                { "pacifier_name", pacifierName },
                { "entry_id", entryId.ToString() }
            };

            // Add the pacifier to the database
            await _databaseService.WriteDataAsync(measurement, fields, tags);

            try
            {
                // Act - Query the database to check if the pacifier exists
                string query = $@"
                from(bucket: ""{_databaseService.Bucket}"")
                |> range(start: -1h)
                |> filter(fn: (r) => r._measurement == ""{measurement}"" 
                    and r.pacifier_name == ""{pacifierName}"" 
                    and r.campaign_name == ""{campaignName}"")";

                var result = await _databaseService.ReadData(query);

                // Assert
                Assert.True(result.Count > 0, $"Expected pacifier '{pacifierName}' to exist in campaign '{campaignName}'.");
            }
            finally
            {
                // Cleanup - Delete the pacifier entry using DeleteEntryAsync
                await DeleteEntryAsync(measurement, entryId);
            }
        }


        [Fact]
        public async Task SensorCycleTest()
        {
            // Arrange
            string campaignName = "Campaign2";
            string pacifierName = "Pacifier1";
            string sensorType = "ppg"; // Sensor type to check
            string measurement = "campaigns";
            int entryId = 4; // Unique ID for the sensor entry
            var fields = new Dictionary<string, object>
            {
                { "led1", 99 },
                { "led2", 102 },
                { "led3", 97 },
                { "temperature", 36.4 },
                { "entry_time", "2024-10-01 12:35:00" }
            };
                    var tags = new Dictionary<string, string>
            {
                { "campaign_name", campaignName },
                { "pacifier_name", pacifierName },
                { "sensor_type", sensorType },
                { "entry_id", entryId.ToString() }
            };

            // Add the sensor to the database
            await _databaseService.WriteDataAsync(measurement, fields, tags);

            try
            {
                // Act - Query the database to check if the sensor exists
                string query = $@"
                from(bucket: ""{_databaseService.Bucket}"")
                |> range(start: -1h)
                |> filter(fn: (r) => r._measurement == ""{measurement}"" 
                and r.sensor_type == ""{sensorType}"" 
                and r.pacifier_name == ""{pacifierName}"" 
                and r.campaign_name == ""{campaignName}"")";

                var result = await _databaseService.ReadData(query);

                // Assert
                Assert.True(result.Count > 0, $"Expected sensor '{sensorType}' to exist for pacifier '{pacifierName}' in campaign '{campaignName}'.");
            }
            finally
            {
                // Cleanup - Delete the sensor entry using DeleteEntryAsync
                await DeleteEntryAsync(measurement, entryId);
            }
        }

        public async Task TestConnection()
        {
            // Arrange
            Assert.NotNull(_databaseService);

                string query = $@"
                from(bucket: ""{_databaseService.Bucket}"")
                |> range(start: -1h)
                |> limit(n: 1)"; // Check if the bucket is accessible

            try
            {
                // Act
                var result = await _databaseService.ReadData(query);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Count > 0, "Expected at least one result from the connection check query.");
            }
            catch (Exception ex)
            {
                // Fail the test if an exception occurs
                Assert.False(true, $"Database connection test failed: {ex.Message}");
            }
        }

        [Fact]
        public async Task TestAPIToken()
        {
            // Arrange: Ensure the _databaseService is initialized
            Assert.NotNull(_databaseService);

            var point = PointData.Measurement("test_measurement")
                .Field("test_field", 1)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            try
            {
                // Act: Write a test data point to the database
                await _databaseService.WriteDataAsync("test_measurement",
                    new Dictionary<string, object> { { "test_field", 1 } },
                    new Dictionary<string, string>());

                // Assert: Validate that the write operation succeeded
                string query = $@"
            from(bucket: ""{_databaseService.Bucket}"")
            |> range(start: -1h)
            |> filter(fn: (r) => r._measurement == ""test_measurement"")";

                var result = await _databaseService.ReadData(query);

                Assert.NotNull(result);
                Assert.True(result.Count > 0, "Expected the test measurement to exist in the database.");
            }
            catch (Exception ex)
            {
                // Fail the test if an exception occurs
                Assert.False(true, $"Expected valid connection and write, but got an error: {ex.Message}");
            }
        }



        // Reusable function for deleting entries
        private async Task DeleteEntryAsync(string measurement, int entryId)
        {
            string deleteQuery = $@"
        from(bucket: ""{_databaseService.Bucket}"")
        |> range(start: -1h)
        |> filter(fn: (r) => r._measurement == ""{measurement}"" and r.entry_id == ""{entryId}"")";

            await _databaseService.DeleteEntryFromDatabaseAsync(entryId, measurement); // Assuming the method signature matches
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
