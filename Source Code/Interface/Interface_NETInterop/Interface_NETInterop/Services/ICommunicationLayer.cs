namespace SmartPacifier.Interface.Services
{   
    public interface ICommunicationLayer
    {
        string ExecuteScript(string pythonCode);
    }
    public interface IBrokerMain
    {
        Task StartAsync(string[] args);
    }
    public interface IBrokerHealthService
    {
        Task<string> CheckBrokerHealthAsync();
        Task<bool> IsBrokerReachableAsync(); // Check if the broker is reachable
        Task<bool> IsReceivingDataAsync();  // Check if the broker is receiving data

    }
}
