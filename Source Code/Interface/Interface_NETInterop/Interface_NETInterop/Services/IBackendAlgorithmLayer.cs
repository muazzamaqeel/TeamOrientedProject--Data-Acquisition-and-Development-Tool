namespace SmartPacifier.Interface.Services
{
    public interface IAlgorithmLayer
    {
        Task<string> ExecuteScriptAsync(string scriptPath, string dataFilePath);
    }
}
