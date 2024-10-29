using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Client;
using MQTTnet.Packets;

namespace SmartPacifier.BackEnd.IOTProtocols
{
    ///<summary>
    ///Broker Class using Singleton Pattern. This class starts the
    /// Mosquitto process uppon creation. The Prefered Way to cleanup
    /// is to use the Dispose() method.
    ///</summary>
    public class Broker : IDisposable
    {
        private readonly string BROKER_ADDRESS = "localhost";
        private readonly int BROKER_PORT = 1883;

        private static Broker? _brokerInstance;
        private static readonly object _lock = new object();

        private IMqttClient _mqttClient;

        private Process? _brokerProcess;
	private bool disposed = false;
	
	// Event handler for received messages
        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
        // Constructor for the Broker class
        private Broker()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            StartBroker();
	    // Register event handler for received messages
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;
        }

	///<summary>
	/// Dispose Method to cleanup the methods.
	///</summary>
	public void Dispose()
	{
	    if (!disposed)
	    {
		StopBroker();
		disposed = true;
	    }
	    // Suppress finalization to avoid calling the destructor
	    // since we are disposing it manually
	    GC.SuppressFinalize(this);
	}

        // Destructor (Finalizer) in case Dispose is not called manually
        ~Broker()
        {
	    Dispose();
        }

	///<summary>
        /// Getting an Instance of the Broker. If there is no instance
        /// yet, it will create one. This is thread-safe for
        /// multithreading
	///</summary>
        public static Broker Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_brokerInstance == null)
                    {
                        _brokerInstance = new Broker();
                    }
                    return _brokerInstance;
                }
            }
        }

        ///<summary>
	/// Starting Mosquitto Process
	///</summary>
        public void  StartBroker()
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "mosquitto", // path to Mosquitto executable
                Arguments = "-v", // Mosquitto command-line arguments
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _brokerProcess = Process.Start(processStartInfo);

            if (_brokerProcess == null)
                Console.WriteLine("Failed to start Mosquitto process.");
        }

        // Read the output from the Mosquitto process (optional)
        public void ReadBrokerOutput()
        {
	    if (_brokerProcess == null)
                return;
            var output = _brokerProcess.StandardOutput.ReadToEnd();
            var error = _brokerProcess.StandardError.ReadToEnd();
            Console.WriteLine("Mosquitto output:\n" + output);
            Console.WriteLine("Mosquitto errors:\n" + error);
        }

        // Stop the Mosquitto process
        public void StopBroker()
        {
	    if(_brokerProcess != null){
		 // Wait 5 seconds
		bool hasExited = _brokerProcess.WaitForExit(5000);
		// Force Kill if process is still running
		if(!hasExited)
		{
		    _brokerProcess.Kill();
		    _brokerProcess.WaitForExit();
		    Console.WriteLine("Mosquitto closed forcefully");
		}
		_brokerProcess.Close();
		_brokerProcess.Dispose();
		
	    }
	    Console.WriteLine("Mosquitto Closed");
        }

        // Connect to the MQTT broker
        public async Task ConnectBroker()
        {
            // Use TCP connection.
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(BROKER_ADDRESS, BROKER_PORT) // Optional port
                .Build();

            try
            {
                await _mqttClient.ConnectAsync(options);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect to MQTT broker: " + ex.Message);
                return;
            }
            Console.WriteLine("Successfully Connected to MQTT broker");
        }

        // Subscribe to a specific topic
        public async Task Subscribe(string topic)
        {
            await _mqttClient.SubscribeAsync(
                new MqttTopicFilterBuilder().WithTopic(topic).Build());

            Console.WriteLine($"Subscribed to topic: {topic}");
        }

        // Unsubscribe from a specific topic
        public async Task Unsubscribe(string topic)
        {
            await _mqttClient.UnsubscribeAsync(topic);

            Console.WriteLine($"Unsubscribed from topic: {topic}");
        }

        // Subscribe to all topics
        public async Task SubscribeToAll()
        {
            await _mqttClient.SubscribeAsync(
                new MqttTopicFilterBuilder().WithTopic("#").Build());

            Console.WriteLine("Subscribed to all topics");
        }
        // Unsubscribe from all topics
        public async Task UnsubscribeFromAll()
        {
            await _mqttClient.UnsubscribeAsync("#");

            Console.WriteLine("Unsubscribed from all topics");
        }

        // Send a message to a specific topic
        public async Task SendMessage(string topic, string message)
        {
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            try
            {
                await _mqttClient.PublishAsync(mqttMessage);
                Console.WriteLine($"Message sent to topic: {topic}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to topic: {topic} - {ex.Message}");
            }
        }
	// Event handler for received messages
        private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            await Task.Run(() =>
            {
                var messageReceivedEventArgs = new MessageReceivedEventArgs(
                e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment));
                MessageReceived?.Invoke(this, messageReceivedEventArgs);
            });

        }

        // Event arguments for received messages
        public class MessageReceivedEventArgs : EventArgs
        {
            public string Topic { get; set; }
            public string Payload { get; set; }

            public MessageReceivedEventArgs(string topic, string payload)
            {
                Topic = topic;
                Payload = payload;
            }
        }
    }
}
