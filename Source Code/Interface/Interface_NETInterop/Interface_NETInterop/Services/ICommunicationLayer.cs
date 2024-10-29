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
}
