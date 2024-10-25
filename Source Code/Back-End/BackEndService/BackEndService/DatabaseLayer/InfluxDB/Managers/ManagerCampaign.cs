using SmartPacifier.Interface.Services;
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

        public async Task<List<string>> ReadData(string query)
        {
            return await _databaseService.ReadData(query);
        }

        /// <summary>
        /// Template function to extract distinct values from a specified column.
        /// This method can be reused to query different data, not just campaigns.
        /// </summary>
        /// <param name="column">The column to get distinct values from (e.g., "campaign_name").</param>
        /// <param name="timeRange">The time range for querying (e.g., "-30d").</param>
        /// <returns>A list of distinct values from the specified column.</returns>
        private async Task<List<string>> GetDistinctValuesFromDBAsync(string column, string timeRange)
        {
            // Define a template query that can extract distinct values from the given column
            var fluxQuery = $"from(bucket: \"SmartPacifier-Bucket1\") " +
                            $"|> range(start: {timeRange}) " +
                            $"|> keep(columns: [\"{column}\"]) " +
                            $"|> distinct(column: \"{column}\")";

            // Use the database service to execute the query and process the results
            var records = await _databaseService.ReadData(fluxQuery);
            var distinctValues = new List<string>();

            foreach (var record in records)
            {
                if (!string.IsNullOrEmpty(record))
                {
                    distinctValues.Add(record);
                }
            }

            return distinctValues;
        }

        /// <summary>
        /// Retrieves all distinct campaign names from the database.
        /// This method reuses the template function to get data from the "campaign_name" column.
        /// </summary>
        /// <returns>A list of distinct campaign names.</returns>
        public async Task<List<string>> GetCampaignsAsync()
        {
            return await GetDistinctValuesFromDBAsync("campaign_name", "-30d");
        }

        /// <summary>
        /// Adds a new campaign to the database.
        /// </summary>
        /// <param name="campaignName">The name of the campaign to add.</param>
        /// <returns></returns>
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
