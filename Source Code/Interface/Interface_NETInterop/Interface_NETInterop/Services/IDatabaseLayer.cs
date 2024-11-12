using InfluxDB.Client;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;

namespace SmartPacifier.Interface.Services
{
    public interface IDatabaseService
    {
        InfluxDBClient GetClient();
        string Bucket { get; }
        string Org { get; }
        Task WriteDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags);
        Task<List<string>> ReadData(string query);
        Task<List<string>> GetCampaignsAsync();
        string Token { get; }
        string BaseUrl { get; }
        Task<DataTable> GetSensorDataAsync();
        Task DeleteEntryFromDatabaseAsync(int entryId);

    }

    public interface IDataManipulationHandler
    {
        Task UpdateRowAsync(
            string measurement,
            Dictionary<string, string> originalTags,
            long originalTimestampNanoseconds,
            Dictionary<string, object> newFields,
            Dictionary<string, string> newTags);
        Task CreateNewEntryAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags);
        Task DeleteRowAsync(string measurement, Dictionary<string, string> tags, long timestampNanoseconds);

    }


    public interface ILocalHost
    {
        void StartDocker();
        void StopDocker();
        string GetApiKey(bool isLocal);
        void DockerInitialize();

    }



    public interface IManagerCampaign
    {
        Task AddCampaignAsync(string campaignName);
        Task StartCampaignAsync(string campaignName);
        Task EndCampaignAsync(string campaignName);
        Task UpdateCampaignAsync(string oldCampaignName, string newCampaignName);
        Task DeleteCampaignAsync(string campaignName);

        // Return CSV data as a string
        Task<string> GetCampaignDataAsCSVAsync();
        Task<List<string>> GetCampaignsAsync();
    }


    public interface IManagerPacifiers
    {

        List<string> GetPacifierNamesFromSensorData();
        Task<List<string>> GetPacifiersAsync(string campaignName);
        Task AddPacifierAsync(string campaignName);

        Task WriteDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags);

    }

    public interface IManagerSensors
    {
        Task AddSensorDataAsync(string pacifierId, float ppgValue, float imuAccelX, float imuAccelY, float imuAccelZ);
    }




    public interface ICSVDataHandler
    {

        void CreateCSV(string campaignName, List<string> pacifierNames);
        void StartCampaign(string campaignName, List<string> pacifierNames);
        void EndCampaign(string campaignName);

    }

    public interface IInfluxDBParser
    {


    }


}
