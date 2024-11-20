using SmartPacifier.Interface.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SmartPacifier.BackEnd.DatabaseLayer.InfluxDB.Components;
using System.Windows;
using System.Diagnostics;


namespace SmartPacifier.BackEnd.Database.InfluxDB.Managers
{
    public class ManagerCampaign : IManagerCampaign
    {
        private readonly IDatabaseService _databaseService;
        private readonly IManagerPacifiers _managerPacifiers;
        private readonly HttpClient _httpClient;
        private readonly string _filePath = @"C:\programming\TeamOrientedProject---Smart-Pacifier\Source Code\Back-End\BackEndService\BackEndService\DatabaseLayer\InfluxDB\LoadFiles\Code\CampaignData.xlsx";
        private readonly InfluxDBParserCSV _parser; // Add InfluxDBParserCSV


        public ManagerCampaign(IDatabaseService databaseService, IManagerPacifiers managerPacifiers)
        {
            _databaseService = databaseService;
            _managerPacifiers = managerPacifiers;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_databaseService.Token}");
            _parser = new InfluxDBParserCSV(); // Initialize InfluxDBParserCSV

        }

        public async Task WriteDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags)
        {
            await _databaseService.WriteDataAsync(measurement, fields, tags);
        }

        public async Task<List<string>> ReadData(string query)
        {
            return await _databaseService.ReadData(query);
        }

        public async Task<List<string>> GetCampaignsAsync()
        {
            return await _databaseService.GetCampaignsAsync();
        }


        public async Task<string> GetCampaignDataAsCSVAsync()
        {
            var query = $"from(bucket:\"{_databaseService.Bucket}\") " +
                        "|> range(start: -1y) " +
                        "|> filter(fn: (r) => r[\"_measurement\"] == \"campaigns\") " +
                        "|> pivot(rowKey:[\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\") " +
                        "|> sort(columns: [\"_time\"], desc: true)";  // Fetch in descending order by time

            // Fetch data from InfluxDB
            var rawCampaignData = await _databaseService.ReadData(query);

            // Convert the raw data to CSV format
            var csvData = _parser.ConvertToCSV(rawCampaignData);

            // Show a message box with the CSV data for verification
            MessageBox.Show(csvData, "CSV Data Generated", MessageBoxButton.OK, MessageBoxImage.Information);

            return csvData;
        }




        /// <summary>
        /// Function 1: Add a new campaign to the database
        /// Function 2: Start a campaign to the database
        /// Function 3: End a campaign to the database
        /// </summary>
        /// <param name="campaignName"></param>
        /// <returns></returns>

        public async Task AddCampaignAsync(string campaignName)
        {
            var tags = new Dictionary<string, string> { { "campaign_name", campaignName } };
            var fields = new Dictionary<string, object>
            {
                { "status", "created" },
                { "creation", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            };
            await WriteCampaignDataAsync("campaigns", fields, tags, DateTime.UtcNow);
        }

        public async Task StartCampaignAsync(string campaignName)
        {
            var tags = new Dictionary<string, string> { { "campaign_name", campaignName } };
            var fields = new Dictionary<string, object>
            {
                { "status", "started" },
                { "start_time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            };
            await WriteCampaignDataAsync("campaigns", fields, tags, DateTime.UtcNow);
        }

        public async Task EndCampaignAsync(string campaignName)
        {
            var tags = new Dictionary<string, string> { { "campaign_name", campaignName } };
            var fields = new Dictionary<string, object>
            {
                { "status", "stopped" },
                { "end_time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            };
            await WriteCampaignDataAsync("campaigns", fields, tags, DateTime.UtcNow);
        }










        /// <summary>
        /// Template to store campaign data in a database
        /// </summary>
        /// <param name="measurement"></param>
        /// <param name="fields"></param>
        /// <param name="tags"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>

        public async Task WriteCampaignDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags, DateTime timestamp)
        {
            try
            {
                var client = _databaseService.GetClient();
                var query = $"from(bucket:\"{_databaseService.Bucket}\") |> range(start: -1y) |> filter(fn: (r) => r[\"_measurement\"] == \"{measurement}\" and r[\"campaign_name\"] == \"{tags["campaign_name"]}\" and r[\"status\"] == \"{fields["status"]}\")";

                // Check if campaign with same name and status already exists
                var existingData = await _databaseService.ReadData(query);
                if (existingData.Count > 0)
                {
                    Console.WriteLine("A campaign with the same name and status already exists in the database.");
                    return;
                }

                // Create PointData object with measurement and timestamp
                var point = PointData.Measurement(measurement)
                    .Timestamp(timestamp, WritePrecision.Ns);

                // Add tags to PointData
                foreach (var tag in tags)
                {
                    point = point.Tag(tag.Key, tag.Value);
                }

                // Add fields to PointData
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

                // Write the data point asynchronously
                var writeApi = client.GetWriteApiAsync();
                await writeApi.WritePointAsync(point, _databaseService.Bucket, _databaseService.Org);

                Console.WriteLine("Data successfully written to InfluxDB.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing data: {ex.Message}");
            }
        }











        /// <summary>
        /// 
        /// ------------------------------------------------
        /// Currently Used for the Developer Tab
        /// ------------------------------------------------
        /// 
        /// Function 1+2: Update the campaign name and pacifiers associated with the campaign
        /// </summary>
        /// <param name="oldCampaignName"></param>
        /// <param name="newCampaignName"></param>
        /// <returns></returns>
        public async Task UpdateCampaignAsync(string oldCampaignName, string newCampaignName)
        {
            // Step 1: Create a new campaign entry with the updated name
            var newCampaignTags = new Dictionary<string, string> { { "campaign_name", newCampaignName } };
            var newCampaignFields = new Dictionary<string, object> { { "status", "active" } };

            await _databaseService.WriteDataAsync("campaigns", newCampaignFields, newCampaignTags);

            // Step 2: Retrieve and update pacifiers associated with the old campaign name to the new name
            var pacifiersToUpdate = await _managerPacifiers.GetPacifiersAsync(oldCampaignName);
            foreach (var pacifierId in pacifiersToUpdate)
            {
                var pacifierTags = new Dictionary<string, string>
        {
            { "campaign_name", newCampaignName },
            { "pacifier_id", pacifierId }
        };
                var pacifierFields = new Dictionary<string, object> { { "status", "assigned" } };

                await _databaseService.WriteDataAsync("pacifiers", pacifierFields, pacifierTags);
            }

            // Step 3: Delete old campaign data after updating to avoid duplicate entries
            await DeleteCampaignAsync(oldCampaignName);
        }
        public async Task DeleteCampaignAsync(string campaignName)
        {
            // Define the delete URL for InfluxDB
            var deleteUrl = $"{_databaseService.BaseUrl}/api/v2/delete?org={_databaseService.Org}&bucket={_databaseService.Bucket}";

            // Set the time range and predicate for deletion
            var deletePayload = new
            {
                start = "1970-01-01T00:00:00Z",
                stop = DateTime.UtcNow.ToString("o"),
                predicate = $"campaign_name=\"{campaignName}\""
            };

            // Serialize the payload
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(deletePayload), Encoding.UTF8, "application/json");

            try
            {
                // Send the delete request
                var response = await _httpClient.PostAsync(deleteUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully deleted campaign '{campaignName}' from InfluxDB.");
                }
                else
                {
                    Console.WriteLine($"Failed to delete campaign '{campaignName}': {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during deletion of campaign '{campaignName}': {ex.Message}");
            }
        }














        public async Task<List<string>> GetPacifiersByCampaignNameAsync(string campaignName)
        {
            var query = $"from(bucket:\"{_databaseService.Bucket}\") " +
                        "|> range(start: -1y) " +
                        $"|> filter(fn: (r) => r[\"_measurement\"] == \"campaigns\" and r[\"campaign_name\"] == \"{campaignName}\") " +
                        "|> keep(columns: [\"pacifier_name\"]) " +
                        "|> distinct(column: \"pacifier_name\")";

            var pacifierData = await _databaseService.ReadData(query);

            var pacifierIds = new List<string>();

            foreach (var data in pacifierData)
            {
                var parsedData = JsonNode.Parse(data);
                if (parsedData?["pacifier_name"] != null)
                {
                    pacifierIds.Add(parsedData["pacifier_name"]?.ToString());
                }
            }

            return pacifierIds;
        }



        public async Task<List<string>> GetSensorsByPacifierNameAsync(string pacifierName, string campaignName)
        {
            var query = $"from(bucket:\"{_databaseService.Bucket}\") " +
                        "|> range(start: -1y) " +
                        $"|> filter(fn: (r) => r[\"_measurement\"] == \"campaigns\" and r[\"pacifier_name\"] == \"{pacifierName}\" and r[\"campaign_name\"] == \"{campaignName}\") " +
                        "|> keep(columns: [\"sensor_type\"])" +
                        "|> distinct(column: \"sensor_type\")"; ;

            var sensorData = await _databaseService.ReadData(query);

            var sensorTypes = new List<string>();

            foreach (var data in sensorData)
            {
                var parsedData = JsonNode.Parse(data);
                if (parsedData?["sensor_type"] != null)
                {
                    sensorTypes.Add(parsedData["sensor_type"]?.ToString());
                }
            }

            return sensorTypes;
        }

        public async Task<List<string>> GetCampaignDataEntriesAsync(string campaignName)
        {
            var query = $"from(bucket:\"{_databaseService.Bucket}\") " +
                        $"|> range(start: 0) " +
                        $"|> filter(fn: (r) => r[\"campaign_name\"] == \"{campaignName}\") " +
                        $"|> filter(fn: (r) => r[\"_field\"] != \"entry_id\") " +
                        $"|> filter(fn: (r) => r[\"_field\"] != \"entry_time\") " +
                        $"|> keep(columns: [\"_time\", \"_value\", \"_field\", \"pacifier_name\", \"sensor_type\"]) " +
                        $"|> sort(columns: [\"_time\"])";

            var campaignData = await _databaseService.ReadData(query);

            return campaignData;
        }

        public async Task<List<string>> GetCampaignsDataAsync()
        {
            var query = $"from(bucket:\"{_databaseService.Bucket}\") " +
                        "|> range(start: 0) " +
                        "|> filter(fn: (r) => r[\"_measurement\"] == \"campaigns\") " +
                        "|> keep(columns: [\"campaign_name\", \"pacifier_name\", \"_time\"])" +
                        "|> group(columns: [\"campaign_name\", \"pacifier_name\"])";

            var campaignData = await _databaseService.ReadData(query);

            var campaignPacifierCount = new Dictionary<string, HashSet<string>>();
            var campaignTimes = new Dictionary<string, (DateTime Min, DateTime Max)>();

            foreach (var data in campaignData)
            {
                var parsedData = JsonNode.Parse(data);
                var campaignName = parsedData?["campaign_name"]?.ToString();
                var pacifierName = parsedData?["pacifier_name"]?.ToString();
                var time = parsedData?["_time"]?.ToString();

                if (!string.IsNullOrEmpty(campaignName) && !string.IsNullOrEmpty(pacifierName) && DateTime.TryParse(time, out var dateTime))
                {
                    if (!campaignPacifierCount.ContainsKey(campaignName))
                    {
                        campaignPacifierCount[campaignName] = new HashSet<string>();
                        campaignTimes[campaignName] = (dateTime, dateTime);
                    }
                    campaignPacifierCount[campaignName].Add(pacifierName);
                    var (minTime, maxTime) = campaignTimes[campaignName];
                    campaignTimes[campaignName] = (dateTime < minTime ? dateTime : minTime, dateTime > maxTime ? dateTime : maxTime);
                }
            }

            var result = new List<string>();

            foreach (var entry in campaignPacifierCount)
            {
                var (startTime, endTime) = campaignTimes[entry.Key];
                result.Add($"Campaign: {entry.Key}, PacifierCount: {entry.Value.Count}, StartTime: {startTime}, EndTime: {endTime}");
            }

            return result;
        }

























        ///-------------------------------------------------------------------------------------------------
        ///Please ignore the following code snippets. They are only for reference purposes.
        ///-------------------------------------------------------------------------------------------------







        /// <summary>
        /// Template to store campaign data in an Excel file
        /// </summary>
        /// <param name="measurement"></param>
        /// <param name="fields"></param>
        /// <param name="tags"></param>
        /// <returns></returns>

        public async Task WriteCampaignDataToExcel(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags)
        {
            await Task.Run(() =>
            {
                // Check if the file exists; create it if not
                using (var workbook = System.IO.File.Exists(_filePath) ? new XLWorkbook(_filePath) : new XLWorkbook())
                {
                    // Check if the worksheet for the measurement exists; if not, create it
                    var worksheet = workbook.Worksheets.Contains(measurement)
                        ? workbook.Worksheet(measurement)
                        : workbook.AddWorksheet(measurement);

                    // Add header if this is a new worksheet
                    if (worksheet.LastRowUsed() == null)
                    {
                        int headerCol = 1;
                        worksheet.Cell(1, headerCol++).Value = "campaign_name";
                        worksheet.Cell(1, headerCol++).Value = "status";
                        worksheet.Cell(1, headerCol++).Value = "creation";
                        worksheet.Cell(1, headerCol++).Value = "start_time";
                        worksheet.Cell(1, headerCol++).Value = "end_time";
                    }

                    // Check if a campaign with the same name and status already exists
                    bool campaignExists = false;
                    foreach (var row in worksheet.RowsUsed())
                    {
                        var campaignName = row.Cell(1).GetString();
                        var status = row.Cell(2).GetString();

                        if (campaignName == tags["campaign_name"] && status == fields["status"].ToString())
                        {
                            campaignExists = true;
                            break;
                        }
                    }

                    if (campaignExists)
                    {
                        Console.WriteLine("Sorry, a campaign with the same name and status already exists.");
                        return;
                    }

                    // Find the next available row
                    var newRow = worksheet.LastRowUsed()?.RowNumber() + 1 ?? 2;

                    // Write campaign_name from tags
                    worksheet.Cell(newRow, 1).Value = tags.ContainsKey("campaign_name") ? tags["campaign_name"].ToString() : "Unknown";

                    // Write status, creation, start_time, end_time fields from fields dictionary
                    worksheet.Cell(newRow, 2).Value = fields.ContainsKey("status") ? fields["status"].ToString() : "Unknown";
                    worksheet.Cell(newRow, 3).Value = fields.ContainsKey("creation") ? fields["creation"].ToString() : "null";
                    worksheet.Cell(newRow, 4).Value = fields.ContainsKey("start_time") ? fields["start_time"].ToString() : "null";
                    worksheet.Cell(newRow, 5).Value = fields.ContainsKey("end_time") ? fields["end_time"].ToString() : "null";

                    // Save the workbook
                    workbook.SaveAs(_filePath);
                }
            });
        }






        ///-------------------------------------------------------------------------------------------------
        ///Please ignore the following code snippets. They are only for reference purposes.
        ///-------------------------------------------------------------------------------------------------

        /*
         

                public async Task WriteCampaignDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags, DateTime timestamp)
        {
            try
            {
                var client = _databaseService.GetClient();
                var point = PointData.Measurement(measurement)
                    .Timestamp(timestamp, WritePrecision.Ns);

                foreach (var tag in tags)
                {
                    point = point.Tag(tag.Key, tag.Value);
                }

                foreach (var field in fields)
                {
                    if (field.Value is float floatValue)
                        point = point.Field(field.Key, floatValue);
                    else if (field.Value is double doubleValue)
                        point = point.Field(field.Key, doubleValue);
                    else if (field.Value is int intValue)
                        point = point.Field(field.Key, intValue);
                    else if (field.Value is string stringValue)
                        point = point.Field(field.Key, stringValue);
                }

                var writeApi = client.GetWriteApiAsync();
                await writeApi.WritePointAsync(point, _databaseService.Bucket, _databaseService.Org);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing data: {ex.Message}");
            }
        }


        */




        public async Task<List<CampaignData>> GetCampaignDataAsync()
        {
            var query = $"from(bucket:\"{_databaseService.Bucket}\") " +
                        "|> range(start: -1y) " +
                        "|> filter(fn: (r) => r[\"_measurement\"] == \"campaigns\") " +
                        "|> pivot(rowKey:[\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\")";

            var rawCampaignData = await _databaseService.ReadData(query);

            var campaignDataList = ParseCampaignData(rawCampaignData);
            return campaignDataList;
        }


        public class CampaignData
        {
            public string CampaignName { get; set; }
            public int PacifierCount { get; set; }
            public string Date { get; set; }
            public string TimeRange { get; set; }
        }


        private List<CampaignData> ParseCampaignData(List<string> rawData)
        {
            var campaigns = new List<CampaignData>();

            foreach (var entry in rawData)
            {
                var data = JsonNode.Parse(entry);

                // Ensure data contains necessary fields
                if (data["campaign_name"] != null && data["start_time"] != null && data["end_time"] != null)
                {
                    var campaign = new CampaignData
                    {
                        CampaignName = data["campaign_name"]?.ToString(),
                        Date = DateTime.Parse(data["start_time"]?.ToString() ?? DateTime.MinValue.ToString())
                            .ToString("MM/dd/yyyy"),
                        TimeRange = $"{DateTime.Parse(data["start_time"]?.ToString() ?? DateTime.MinValue.ToString()).ToString("hh:mm tt")} - " +
                                    $"{DateTime.Parse(data["end_time"]?.ToString() ?? DateTime.MinValue.ToString()).ToString("hh:mm tt")}",
                        //PacifierCount = GetPacifierCount(data["campaign_name"]?.ToString())
                    };

                    campaigns.Add(campaign);
                }
            }

            return campaigns;
        }




    }
}
