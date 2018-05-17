using System.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetFrameworkDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine(" ---  .NET FRAMEWORK  ---  .NET FRAMEWORK --- .NET FRAMEWORK --- .NET FRAMEWORK ---");
			SortedSet();
			TakeFifth();
			Compress();
			//DoMath();
			Serialization();
			IndexOf();
			//Concurrency();
			Networking();
		}

		static void SortedSet()
		{
			Console.WriteLine("Listor - Skapar 200.000 objekt i en sorterad lista.");
			Console.ReadKey();
			var sw = Stopwatch.StartNew();
			var ss = new SortedSet<int>(Enumerable.Repeat(42, 200_000));
			Console.WriteLine(sw.Elapsed);
			Console.ReadKey();
			Console.WriteLine();
		}

		static void TakeFifth()
		{
			Console.WriteLine("Listor - Skapar 10.000.000 objekt och därefter sorterar ut var femte. (2ggr)");
			Console.ReadKey();
			IEnumerable<int> tenMillionToZero = Enumerable.Range(0, 10_000_000).Reverse();
			for (int i = 0; i < 2; i++)
			{
				var sw = Stopwatch.StartNew();
				int fifth = tenMillionToZero.OrderBy(y => y).Skip(4).First();
				Console.WriteLine(sw.Elapsed);
			}
			Console.ReadKey();
			Console.WriteLine();
		}

		static void Compress()
		{
			Console.WriteLine("Komprimering - Komprimerar och dekomprimerar 100MB data.");
			Console.ReadKey();
			// Create some fairly compressible data
			byte[] raw = new byte[100 * 1024 * 1024];
			for (int i = 0; i < raw.Length; i++) raw[i] = (byte)i;
			var sw = Stopwatch.StartNew();

			// Compress it
			var compressed = new MemoryStream();
			using (DeflateStream ds = new DeflateStream(compressed, CompressionMode.Compress, true))
			{
				ds.Write(raw, 0, raw.Length);
			}
			compressed.Position = 0;

			// Decompress it
			var decompressed = new MemoryStream();
			using (DeflateStream ds = new DeflateStream(compressed, CompressionMode.Decompress))
			{
				ds.CopyTo(decompressed);
			}
			decompressed.Position = 0;

			Console.WriteLine(sw.Elapsed);
			Console.ReadKey();
			Console.WriteLine();
		}

		private static void DoMath()
		{
			Console.WriteLine("Skapar 3 stora tal med ModPow.");
			Console.ReadKey();
			var rand = new Random(42);
			BigInteger a = Create(rand, 8192);
			BigInteger b = Create(rand, 8192);
			BigInteger c = Create(rand, 8192);

			var sw = Stopwatch.StartNew();
			BigInteger.ModPow(a, b, c);
			Console.WriteLine(sw.Elapsed);
			Console.ReadKey();
			Console.WriteLine();
		}

		private static BigInteger Create(Random rand, int bits)
		{
			var value = new byte[(bits + 7) / 8 + 1];
			rand.NextBytes(value);
			value[value.Length - 1] = 0;
			return new BigInteger(value);
		}

		static void Concurrency()
		{
			Console.WriteLine("Concurrency och minneshantering. (2ggr)");
			Console.ReadKey();
			for (int y = 0; y < 2; y++)
			{
				int remaining = 20_000_000;
				var mres = new ManualResetEventSlim();
				WaitCallback wc = null;
				wc = delegate
				{
					if (Interlocked.Decrement(ref remaining) <= 0) mres.Set();
					else ThreadPool.QueueUserWorkItem(wc);
				};

				var sw = new Stopwatch();
				int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
				sw.Start();

				for (int i = 0; i < Environment.ProcessorCount; i++) ThreadPool.QueueUserWorkItem(wc);
				mres.Wait();

				Console.WriteLine($"Elapsed={sw.Elapsed} Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}");
			}
			Console.ReadKey();
			Console.WriteLine();
		}

		static void Serialization()
		{
			Console.WriteLine("Serializering - Skapar 200.000 objekt och som sedan serializeras och deserializeras.");
			Console.ReadKey();
			var books = new List<Book>();
			for (int i = 0; i < 200_000; i++)
			{
				string id = i.ToString();
				books.Add(new Book { Name = id, Id = id });
			}

			var formatter = new BinaryFormatter();
			var mem = new MemoryStream();
			formatter.Serialize(mem, books);
			mem.Position = 0;

			var sw = Stopwatch.StartNew();
			formatter.Deserialize(mem);
			sw.Stop();

			Console.WriteLine(sw.Elapsed.TotalSeconds);
			Console.ReadKey();
			Console.WriteLine();
		}

		static void IndexOf()
		{
			Console.WriteLine("Strängar - Skapar strängar och gör IndexOf.  (3ggr)");
			Console.ReadKey();
			for (int y = 0; y < 3; y++)
			{

				string s = string.Concat(Enumerable.Repeat("a", 100)) + "b";
				var sw = Stopwatch.StartNew();
				for (int i = 0; i < 100_000_000; i++)
				{
					s.IndexOf('b');
				}
				Console.WriteLine(sw.Elapsed);
			}
			Console.ReadKey();
			Console.WriteLine();
		}

		static void Networking()
		{
			Console.WriteLine("Nätverk - Skapar two sockets och skriver 1.000.000 gånger.");
			Console.ReadKey();
			using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
				listener.Listen(1);

				Task connectTask = Task.Run(() => client.Connect(listener.LocalEndPoint));
				using (Socket server = listener.Accept())
				{
					connectTask.Wait();

					using (var clientAre = new AutoResetEvent(false))
					using (var clientSaea = new SocketAsyncEventArgs())
					using (var serverAre = new AutoResetEvent(false))
					using (var serverSaea = new SocketAsyncEventArgs())
					{
						byte[] sendBuffer = new byte[1000];
						clientSaea.SetBuffer(sendBuffer, 0, sendBuffer.Length);
						clientSaea.Completed += delegate { clientAre.Set(); };

						byte[] receiveBuffer = new byte[1000];
						serverSaea.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
						serverSaea.Completed += delegate { serverAre.Set(); };

						var sw = new Stopwatch();
						int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
						sw.Start();

						for (int i = 0; i < 1_000_000; i++)
						{
							if (client.SendAsync(clientSaea)) clientAre.WaitOne();
							if (clientSaea.SocketError != SocketError.Success) throw new SocketException((int)clientSaea.SocketError);

							if (server.ReceiveAsync(serverSaea)) serverAre.WaitOne();
							if (serverSaea.SocketError != SocketError.Success) throw new SocketException((int)clientSaea.SocketError);
						}

						Console.WriteLine($"Elapsed={sw.Elapsed} Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}");
					}
				}
			}

			Console.WriteLine();
			Console.ReadKey();
		}
	}

	[Serializable]
	public class Book
	{
		public string Name;
		public string Id;
	}
}
