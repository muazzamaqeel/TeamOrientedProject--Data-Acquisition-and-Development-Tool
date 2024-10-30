using SmartPacifier.BackEnd.CommunicationLayer.MQTT;
using System;


class TestClass
{
    public static async Task Main(string[] args)
    {
        Broker broker = Broker.Instance;
        broker.MessageReceived += (sender, e) =>
        {
            Console.WriteLine(
                $"Received message: Topic={e.Topic}, Payload={e.Payload}");
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
        await broker.SendMessage("SmartPacifier", "Hi from SmartPacifier Project");

        broker.Dispose();

        // Keep the console open
        Console.WriteLine("Press Enter to exit...");
        Thread.Sleep(60000);
        Thread.Sleep(60000);
        Thread.Sleep(60000);
    }
}
