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


namespace SmartPacifier.BackEnd.Database.InfluxDB.Managers
{
    public class ManagerCampaign : IManagerCampaign
    {
        private readonly IDatabaseService _databaseService;
        private readonly IManagerPacifiers _managerPacifiers;
        private readonly HttpClient _httpClient;

        public ManagerCampaign(IDatabaseService databaseService, IManagerPacifiers managerPacifiers)
        {
            _databaseService = databaseService;
            _managerPacifiers = managerPacifiers;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_databaseService.Token}");
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





        /// <summary>
        /// Function 1: Add a new campaign to the database
        /// Function 2: Start a campaign to the database
        /// Function 3: End a campaign to the database
        /// </summary>
        /// <param name="campaignName"></param>
        /// <returns></returns>

        public async Task AddCampaignAsync(string campaignName)
        {
            var tags = new Dictionary<string, string>
                {
                    { "campaign_name", campaignName }
                };

            var fields = new Dictionary<string, object>
                {
                    { "status", "created" },
                    { "creation", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") },
                    { "start_time", "null" },
                    { "end_time", "null" }
                };

            await WriteCampaignDataAsync("campaigns", fields, tags, DateTime.UtcNow);
        }

        public async Task StartCampaignAsync(string campaignName)
        {
            var tags = new Dictionary<string, string>
            {
                { "campaign_name", campaignName }
            };

                    var fields = new Dictionary<string, object>
            {
                { "status", "started" },
                { "start_time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            };

            await WriteCampaignDataAsync("campaigns", fields, tags, DateTime.UtcNow);
        }
        public async Task EndCampaignAsync(string campaignName)
        {
            var tags = new Dictionary<string, string>
                {
                    { "campaign_name", campaignName }
                };

                        var fields = new Dictionary<string, object>
                {
                    { "status", "stopped" },
                    { "end_time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
                };

            await WriteCampaignDataAsync("campaigns", fields, tags, DateTime.UtcNow);
        }







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

























    }
}
