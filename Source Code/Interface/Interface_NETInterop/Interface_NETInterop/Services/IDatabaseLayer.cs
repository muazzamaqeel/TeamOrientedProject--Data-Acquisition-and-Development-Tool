using InfluxDB.Client;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    }


    public interface ILocalHost
    {
        void StartDocker();
        void StopDocker();
        string GetApiKey();
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
        Task<List<string>> GetPacifiersAsync(string campaignName);
        Task AddPacifierAsync(string campaignName);

        Task WriteDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags);

    }

    public interface IManagerSensors
    {
        Task AddSensorDataAsync(string pacifierId, float ppgValue, float imuAccelX, float imuAccelY, float imuAccelZ);
    }

    public interface IInfluxDBParser
    {


    }
}
