namespace SmartPacifier.Interface.Services
{
    public interface IDatabaseService
    {
        Task WriteDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags); // Updated to Task
        Task<List<string>> ReadData(string query);  // Updated to be async
        Task<List<string>> GetCampaignsAsync();  // Asynchronous method to retrieve campaigns
    }

    public interface IManagerCampaign : IDatabaseService
    {
        Task AddCampaignAsync(string campaignName);  // Campaign-specific method
    }

    public interface IManagerPacifiers
    {
        Task<List<string>> GetPacifiersAsync(string campaignName);  // Gets pacifiers by campaign
        Task AddPacifierAsync(string campaignName, string pacifierId);  // Adds a pacifier

        // Missing methods
        Task<List<string>> GetCampaignsAsync();  // Gets all campaigns
        Task<List<string>> ReadData(string query);  // Reads data from InfluxDB
        Task WriteDataAsync(string measurement, Dictionary<string, object> fields, Dictionary<string, string> tags);  // Writes data to InfluxDB
    }

    public interface IManagerSensors : IDatabaseService
    {
        Task AddSensorDataAsync(string pacifierId, float ppgValue, float imuAccelX, float imuAccelY, float imuAccelZ);  // Sensor-specific method
    }
}
