using SmartPacifier.BackEnd.IOTProtocols;
using System;

class TestClass
{
    static async Task Main(string[] args)
    {
	Broker broker = Broker.Instance;
	broker.MessageReceived += (sender, e) =>
	{
	    Console.WriteLine(
		$"Recieved message: Topic={e.Topic}, Payload={e.Payload}");
	};
	await broker.ConnectBroker();
	await broker.SubscribeToAll();
	Thread.Sleep(10000);
	await broker.UnsubscribeFromAll();
	Thread.Sleep(10000);

	await broker.Subscribe("lamp");
	Thread.Sleep(10000);
	await broker.Unsubscribe("lamp/1");
	
	await broker.Subscribe("temperature");
	Thread.Sleep(10000);
	await broker.Unsubscribe("temperature");

	await broker.Subscribe("SmartPacifier");
	await broker.SendMessage("SmartPacifier",
				 "Hi from SmartPacifier Project");
	
	broker.Dispose();
    }
}
