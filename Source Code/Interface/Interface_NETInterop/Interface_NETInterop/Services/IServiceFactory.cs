namespace SmartPacifier.Interface.Services
{


    /// <summary>
    /// The IServiceFactory interface is used to create the services that are needed by the backend
    /// </summary>
    public interface IServiceFactory

    {
        IAlgorithmLayer CreateAlgorithmService();
        IDatabaseService CreateDatabaseService(string url, string token);
    }
}
