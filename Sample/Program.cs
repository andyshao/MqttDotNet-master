using System;
using System.Collections.Generic;
using System.Text;

using MqttLib;

namespace Sample
{
	class Program
	{
		static void Main(string[] args)
		{
			//if (args.Length != 2)
			//{

			//	Console.WriteLine("Usage: " + Environment.GetCommandLineArgs()[0] + " ConnectionString ClientId");
			//	return;
			//}

			Console.WriteLine("Starting MqttDotNet sample program.");
			Console.WriteLine("Press any key to stop\n");

            String clientID = Guid.NewGuid().ToString().Substring(0,23);
            String connString = "tcp://172.16.0.55:61613";


            Program prog = new Program(connString, clientID);
			prog.Start();

			Console.ReadKey();
			prog.Stop();
		}

		IMqtt _client;

		Program(string connectionString, string clientId)
		{
			// Instantiate client using MqttClientFactory
			_client = MqttClientFactory.CreateClient(connectionString, clientId);

			// Setup some useful client delegate callbacks
			_client.Connected += new ConnectionDelegate(client_Connected);
			_client.ConnectionLost += new ConnectionDelegate(_client_ConnectionLost);
			_client.PublishArrived += new PublishArrivedDelegate(client_PublishArrived);
		}

		void Start()
		{
			// Connect to broker in 'CleanStart' mode
			Console.WriteLine("Client connecting\n");
			_client.Connect(true);
		}

		void Stop()
		{
			if (_client.IsConnected)
			{
				Console.WriteLine("Client disconnecting\n");
				_client.Disconnect();
				Console.WriteLine("Client disconnected\n");
			}
		}

		void client_Connected(object sender, EventArgs e)
		{
			Console.WriteLine("Client connected\n");
			RegisterOurSubscriptions();
			PublishSomething();
		}

		void _client_ConnectionLost(object sender, EventArgs e)
		{
			Console.WriteLine("Client connection lost\n");
		}

		void RegisterOurSubscriptions()
		{
			Console.WriteLine("Subscribing to mqttdotnet/test\n");
			_client.Subscribe("mqttdotnet/test", QoS.BestEfforts);
		}

		void PublishSomething()
		{
			Console.WriteLine("Publishing on mqttdotnet/test\n");
			_client.Publish("mqttdotnet/test", "Hello MQTT World", QoS.OnceAndOnceOnly, false);
		}

		bool client_PublishArrived(object sender, PublishArrivedArgs e)
		{
			Console.WriteLine("Received Message");
			Console.WriteLine("Topic: " + e.Topic);
			Console.WriteLine("Payload: " + e.Payload);
			Console.WriteLine();
			return true;
		}

	}

}
