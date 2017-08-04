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

		//static ArrayList clientAs = ArrayList.Synchronized(new ArrayList());
		//static ArrayList clientBs = ArrayList.Synchronized(new ArrayList());
		static List<ClientWithState> clientAs = new List<ClientWithState>();
		static List<ClientWithState> clientBs = new List<ClientWithState>();
		//static List<Thread> threadAs = new List<Thread>();
		//static List<Thread> threadBs = new List<Thread>();

		static List<ManualResetEvent> manualEvents = new List<ManualResetEvent>();

		static void Main(string[] args)
		{
			//if (args.Length != 2)
			//{

			//	Console.WriteLine("Usage: " + Environment.GetCommandLineArgs()[0] + " ConnectionString ClientId");
			//	return;
			//}
			Console.WriteLine("Starting MqttDotNet sample program.");
			//Console.WriteLine("Press any key to stop\n");

			for (int i = 1; i <= 20; i++)
			{
				var A = new ClientWithState("A", i, 2);
				clientAs.Add(A);
				//threadAs.Add(new Thread(A.PublishSometimes));

				var B = new ClientWithState("B", i, 2);
				clientBs.Add(B);
				//threadBs.Add(new Thread(B.PublishSometimes));
			}
			foreach (ClientWithState clientA in clientAs)
				clientA.ClientInit();
			foreach (ClientWithState clientB in clientBs)
				clientB.ClientInit();


			foreach (ClientWithState clientA in clientAs)
			{
				ManualResetEvent mre = new ManualResetEvent(false);
				manualEvents.Add(mre);
				ThreadPool.QueueUserWorkItem(clientA.PublishSometimes, mre);
			}
			foreach (ClientWithState clientB in clientBs)
			{
				ManualResetEvent mre = new ManualResetEvent(false);
				manualEvents.Add(mre);
				ThreadPool.QueueUserWorkItem(clientB.PublishSometimes, mre);
			}
			//foreach (Thread threadA in threadAs)
			//	threadA.Start();
			//foreach (Thread threadB in threadBs)
			//	threadB.Start();

			//等待线程全部结束
			WaitHandle.WaitAll(manualEvents.ToArray());
			Thread.Sleep(5000);
			PublishFailedSum();
		}
		
		static void PublishFailedSum()
		{
			long publishArrivedSum = 0, publishSum = 0;
			for (int i = 0; i < clientAs.Count; i++)
			{
				publishSum += clientAs[i].publishTimes;
				publishArrivedSum += clientBs[i].publishArrivedTimes;
			}
			long publishFailedSum = publishSum - publishArrivedSum;
			Console.WriteLine("publishFailedSum of clientAs is {0}\n{1}\n{2}", publishFailedSum, publishSum, publishArrivedSum);
		}


		class ClientWithState
		{

			private string clientGroup;
			private int clientNum;
			private int _publishTimes;
			private int _publishArrivedTimes;

			public int publishTimes
			{
				get	{ return _publishTimes; }
				set	{ _publishTimes = value; }
			}
			public int publishArrivedTimes
			{
				get { return _publishArrivedTimes; }
				//set { _publishArrivedTimes = 0; }
			}

			public ClientWithState(string group, int num, int t)
			{
				clientGroup = group;
				clientNum = num;
				publishTimes = t;
			}

			String clientID = Guid.NewGuid().ToString();//.Substring(0,23);
			String connString = "tcp://127.0.0.1:11883";
			IMqtt _client;

			public void ClientInit()
			{
				// Instantiate client using MqttClientFactory
				_client = MqttClientFactory.CreateClient(connString, clientID);

				// Setup some useful client delegate callbacks
				_client.Connected += new ConnectionDelegate(client_Connected);
				_client.ConnectionLost += new ConnectionDelegate(_client_ConnectionLost);
				_client.PublishArrived += new PublishArrivedDelegate(client_PublishArrived);
				Start();
			}

			public void PublishSometimes(object obj)
			{
				lock (this)
				{
					for (int i = 1; i <= _publishTimes; i++)
						this.PublishSomething();
				}
				ManualResetEvent mre = (ManualResetEvent)obj;
				mre.Set();

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
				//PublishSomething();
			}
			void _client_ConnectionLost(object sender, EventArgs e)
			{
				Console.WriteLine("Client connection lost\n");
				//this.Start();
			}

			void RegisterOurSubscriptions()
			{
				Console.WriteLine("Subscribing to mqttdotnet/" + clientGroup + clientNum.ToString() + "\n");
				_client.Subscribe("mqttdotnet/" + clientGroup + clientNum.ToString(), QoS.BestEfforts);
				Console.WriteLine("Subscribed");
			}

			public void PublishSomething()
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
				_publishArrivedTimes++;
				return true;
			}

		}
	}

}
