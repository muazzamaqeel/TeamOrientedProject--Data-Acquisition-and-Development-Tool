using SmartPacifier.Interface.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartPacifier.BackEnd.Database.InfluxDB.Managers
{
    public class ManagerCampaign : IManagerCampaign
    {
        private readonly IDatabaseService _databaseService;

        public ManagerCampaign(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task WriteDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags)
        {
            await _databaseService.WriteDataAsync(measurement, fields, tags);
        }

        public List<string> ReadData(string query)
        {
            return _databaseService.ReadData(query);
        }

        public async Task<List<string>> GetCampaignsAsync()
        {
            return await _databaseService.GetCampaignsAsync();
        }

        public async Task AddCampaignAsync(string campaignName)
        {
            var tags = new Dictionary<string, string>
            {
                { "campaign_name", campaignName }
            };

            var fields = new Dictionary<string, object>
            {
                { "status", "active" }
            };

            await WriteDataAsync("campaigns", fields, tags);
        }
    }
}
