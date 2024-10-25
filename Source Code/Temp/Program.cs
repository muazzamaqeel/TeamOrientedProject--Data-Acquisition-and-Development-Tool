using SmartPacifier.BackEnd.IOTProtocols;
using System;

class TestClass
{
    static async Task Main(string[] args)
    {
	Broker broker = Broker.Instance;
	await broker.ConnectBroker();
	await broker.SubscribeToAll();
	broker.StopBroker();
	int i= 0;
	while(i < 15){
	    //broker.ReadBrokerOutput();
	    Thread.Sleep(1000);
	    Console.WriteLine(i);
	    i++;
	}
	GC.Collect();
	GC.WaitForPendingFinalizers();
    }
}
