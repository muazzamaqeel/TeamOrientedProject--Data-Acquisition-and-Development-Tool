using SmartPacifier.BackEnd.Database.InfluxDB.Managers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace SmartPacifier___TestingFramework.UnitTests.UTBackEnd.UTManagers
{
    public class ManagerCampaignWrapper
    {
        private readonly ManagerCampaign _managerCampaign;
        private readonly string _bucket;

        public ManagerCampaignWrapper(ManagerCampaign managerCampaign, string bucket)
        {
            _managerCampaign = managerCampaign;
            _bucket = bucket; // Explicitly set the bucket name in the wrapper
        }

        public async Task<bool> CampaignExistsAsync(string campaignName)
        {
            var query = $"from(bucket: \"{_bucket}\") " +
                        $"|> range(start: -30d) " +
                        $"|> filter(fn: (r) => r[\"campaign_name\"] == \"{campaignName}\")";

            var records = await _managerCampaign.ReadData(query);
            return records.Any();
        }

        public async Task<List<string>> GetPacifiersInCampaignAsync(string campaignName)
        {
            var query = $"from(bucket: \"{_bucket}\") " +
                        $"|> range(start: -30d) " +
                        $"|> filter(fn: (r) => r[\"campaign_name\"] == \"{campaignName}\") " +
                        $"|> keep(columns: [\"pacifier_id\"]) " +
                        $"|> distinct(column: \"pacifier_id\")";

            var records = await _managerCampaign.ReadData(query);
            return records?.Where(r => !string.IsNullOrEmpty(r)).ToList() ?? new List<string>();
        }
    }
}
