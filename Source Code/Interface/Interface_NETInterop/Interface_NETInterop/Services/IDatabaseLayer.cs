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
        Task DeleteEntryFromDatabaseAsync(int entryId, string measurement);
        Task<Dictionary<string, object>> GetCampaignDataAlgorithmLayerAsync(string campaignName);
        Task UploadDataUsingLineProtocolAsync(IEnumerable<string> lineProtocolData);


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

        Task WriteDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags);
        Task<List<string>> ReadData(string query);

        Task<List<string>> GetPacifiersByCampaignNameAsync(string campaignName);
        Task<List<string>> GetSensorsByPacifierNameAsync(string pacifierName, string campaignName);
        Task<List<string>> GetCampaignDataEntriesAsync(string campaignName);
        Task<List<string>> GetCampaignsDataAsync();
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


    public interface ILineProtocol
    {
        void CreateFileCamp(string campaignName, int pacifierCount, string entryTime);
        void AppendToCampaignFile(string campaignName, int pacifierCount, string pacifierName, string sensorType, List<Dictionary<string, object>> parsedData, string entryTime);
        void UpdateStoppedEntryTime(string campaignName, string newEndTime);
        Task SendFileDataToDatabaseAsync(string campaignName); // Ensure this matches the FileManager method signature


    }


}
