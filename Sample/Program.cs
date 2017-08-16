using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MqttLib;
using System.Collections;

namespace Sample
{
	class Program
	{
		static int clientNum = 100;
		static int publishNum = 10;
		static long publishArrivedCount = 0;
		static int subFinishCount = 0;
		static int pubFinishCount = 0;
		static readonly object locker = new object();

		static void Main(string[] args)
		{
			//if (args.Length != 2)
			//{

			//	Console.WriteLine("Usage: " + Environment.GetCommandLineArgs()[0] + " ConnectionString ClientId");
			//	return;
			//}
			Console.WriteLine("Starting MqttDotNet sample program.");
			//Console.WriteLine("Press any key to stop\n");

			List<Program> clientAs = new List<Program>();
			List<Program> clientBs = new List<Program>();

			String connString = "tcp://127.0.0.1:11883";

			for (int i = 1; i <= clientNum; i++)
			{
				String clientID = Guid.NewGuid().ToString().Substring(0,23);
				var A = new Program(connString, clientID, "A", i);
				clientAs.Add(A);
				new Thread(A.Start).Start();
			}
			for (int i = 1; i <= clientNum; i++)
			{
				String clientID = Guid.NewGuid().ToString().Substring(0,23);
				var B = new Program(connString, clientID, "B", i);
				clientAs.Add(B);
				new Thread(B.Start).Start();;
			}

			while (subFinishCount != clientNum * 2)
				;
			Console.WriteLine("****************Client Subscribe All Finished!****************\n");
			Thread.Sleep(3000);

			foreach (Program clientA in clientAs)
			{
				for (int i = 1; i <= publishNum; i++)
				{
					new Thread(clientA.PublishSomething).Start();
				}
			}
			foreach (Program clientB in clientBs)
			{
				for (int i = 1; i <= publishNum; i++)
				{
					new Thread(clientB.PublishSomething).Start();
				}
			}

			long publishNumSum = clientNum * 2 * publishNum;
			while (pubFinishCount != publishNumSum)
				;
			Thread.Sleep(1000);
			Console.WriteLine("****************Client Publish All Finished!****************\n");
			//double lossRate = (publishNumSum - publishArrivedCount) / publishNumSum;
			//Console.WriteLine("The Loss Rate is {0}%", (lossRate * 100).ToString("0.00"));
			long publishLostCount = publishNumSum - publishArrivedCount;
			Console.WriteLine("The Lost Count Is {0}", publishLostCount.ToString());
		}

		IMqtt _client;
		string clientGroup { get; set; }
		int clientCount { get; set; }

		Program(string connectionString, string clientId, string group, int count)
		{
			clientGroup = group;
			clientCount = count;
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
			Console.WriteLine("Client {0}{1} connecting", clientGroup, clientCount.ToString());
			_client.Connect(true);
		}

		void Stop()
		{
			if (_client.IsConnected)
			{
				Console.WriteLine("Client disconnecting");
				_client.Disconnect();
				Console.WriteLine("Client disconnected");
			}
		}

		void client_Connected(object sender, EventArgs e)
		{
			lock (locker)
			{
				Console.WriteLine("Client {0}{1} connected", clientGroup, clientCount.ToString());
				RegisterOurSubscriptions();
				//PublishSomething();
			}
		}
		void _client_ConnectionLost(object sender, EventArgs e)
		{
			lock (locker)
			{
				Console.WriteLine("Client connection lost");
				Start();
				subFinishCount--;
			}
		}

		void RegisterOurSubscriptions()
		{
			lock (locker)
			{
				Console.WriteLine("Subscribing to mqttdotnet/" + clientGroup + clientCount.ToString());
				_client.Subscribe("mqttdotnet/" + clientGroup + clientCount.ToString(), QoS.BestEfforts);
				subFinishCount++;
			}
		}

		void PublishSomething()
		{
			lock (locker)
			{
				string _clientGroup = (clientGroup == "A") ? "B" : "A";
				Console.WriteLine("Publishing on mqttdotnet/" + _clientGroup + clientCount.ToString());
				_client.Publish("mqttdotnet/" + _clientGroup + clientCount.ToString(), "Hello MQTT World", QoS.OnceAndOnceOnly, false);
				pubFinishCount++;
			}
		}

		bool client_PublishArrived(object sender, PublishArrivedArgs e)
		{
			lock (locker)
			{
				//Console.WriteLine("Received Message");
				//Console.WriteLine("Topic: " + e.Topic);
				//Console.WriteLine("Payload: " + e.Payload);
				//Console.WriteLine();
				publishArrivedCount++;
				return true;
			}
		}
	}
}
