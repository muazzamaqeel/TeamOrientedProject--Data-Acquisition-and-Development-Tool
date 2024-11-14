namespace SmartPacifier.Interface.Services
{
    public interface IAlgorithmLayer
    {
        string ExecuteScript(string scriptNameOrCode, string campaignName);
    }
}
