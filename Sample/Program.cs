using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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

			List<ThreadWithState> clientListsA = new List<ThreadWithState>();
			List<ThreadWithState> clientListsB = new List<ThreadWithState>();

			int n;
			for (n = 1; n <= 2; n++)
			{
				var twsA = new ThreadWithState("A", n);
				var tA = new Thread(twsA.ClientThread);
				tA.Start();
			}

			
			//Thread.Sleep(5000);
			//for (n = 1; n <= 2; n++)
			//	{
			//		var twsB = new ThreadWithState("B", n);
			//	var tB = new Thread(twsB.ClientThread);
			//	tB.Start();
			//}

		}

		public class ThreadWithState
		{

			private string clientGroup;
			private int clientNum;

			public ThreadWithState(string group, int num)
			{
				clientGroup = group;
				clientNum = num;
			}

			String clientID = Guid.NewGuid().ToString();//.Substring(0,23);
			String connString = "tcp://127.0.0.1:11883";
			IMqtt _client;

			public void ClientThread()
			{
				_client = MqttClientFactory.CreateClient(connString, clientID);
				_client.Connected += new ConnectionDelegate(client_Connected);
				_client.ConnectionLost += new ConnectionDelegate(_client_ConnectionLost);
				_client.PublishArrived += new PublishArrivedDelegate(client_PublishArrived);
				Start();
			}
			public void Start()
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
				//PublishSomething();
			}
			void _client_ConnectionLost(object sender, EventArgs e)
			{
				Console.WriteLine("Client connection lost\n");
				this.Start();
			}

			void RegisterOurSubscriptions()
			{
				Console.WriteLine("Subscribing to mqttdotnet/" + clientGroup + clientNum.ToString() + "\n");
				_client.Subscribe("mqttdotnet/" + clientGroup + clientNum.ToString(), QoS.BestEfforts);
				Console.WriteLine("Subscribed");
			}

			void PublishSomething()
			{
				string clientGroup_= (clientGroup == "A")?"B":"A";
				Console.WriteLine("Publishing on mqttdotnet/" + clientGroup_ + clientNum.ToString() + "\n");
				_client.Publish("mqttdotnet/" + clientGroup_ + clientNum.ToString(), "Hello MQTT World", QoS.OnceAndOnceOnly, false);
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

}
