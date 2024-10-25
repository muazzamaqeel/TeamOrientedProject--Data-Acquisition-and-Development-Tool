using InfluxDB.Client;
using SmartPacifier.Interface.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        // Delegating to the injected _databaseService
        public Task WriteDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags)
        {
            return _databaseService.WriteDataAsync(measurement, fields, tags);
        }

        public List<string> ReadData(string query)
        {
            return _databaseService.ReadData(query);
        }

        public Task<List<string>> GetCampaignsAsync()
        {
            return _databaseService.GetCampaignsAsync();
        }

        // ManagerPacifiers-specific method to add a pacifier
        public async Task AddPacifierAsync(string campaignName, string pacifierId)
        {
            var tags = new Dictionary<string, string>
            {
                { "campaign_name", campaignName },
                { "pacifier_id", pacifierId }
            };

            var fields = new Dictionary<string, object>
            {
                { "status", "assigned" }
            };

            await WriteDataAsync("pacifiers", fields, tags);
        }

        // ManagerPacifiers-specific method to get pacifiers
        public async Task<List<string>> GetPacifiersAsync(string campaignName)
        {
            var pacifiers = new List<string>();
            try
            {
                var fluxQuery = $"from(bucket: \"{_bucket}\") " +
                                $"|> range(start: -30d) " +
                                $"|> filter(fn: (r) => r[\"_measurement\"] == \"pacifiers\" and r[\"campaign_name\"] == \"{campaignName}\") " +
                                $"|> keep(columns: [\"pacifier_id\"]) " +
                                $"|> distinct(column: \"pacifier_id\")";

                var queryApi = _client.GetQueryApi();
                var tables = await queryApi.QueryAsync(fluxQuery, _org);

                foreach (var table in tables)
                {
                    foreach (var record in table.Records)
                    {
                        var pacifierId = record.GetValueByKey("pacifier_id")?.ToString();
                        if (!string.IsNullOrEmpty(pacifierId))
                        {
                            pacifiers.Add(pacifierId);
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
    }
}
