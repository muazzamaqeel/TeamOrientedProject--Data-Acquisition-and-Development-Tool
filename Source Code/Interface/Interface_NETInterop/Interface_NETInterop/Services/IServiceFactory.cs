using SmartPacifier.Interface.Services;

namespace SmartPacifier.BackEnd
{
    public interface IServiceFactory
    {
        IDatabaseService CreateDatabaseService(string url, string token, string bucket, string org);
    }
}
