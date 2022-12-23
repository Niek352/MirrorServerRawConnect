using kcp2k;
using Mirror;

public class Program
{
	static TransportError ToTransportError(ErrorCode error)
	{
		switch(error)
		{
			case ErrorCode.DnsResolve: return TransportError.DnsResolve;
			case ErrorCode.Timeout: return TransportError.Timeout;
			case ErrorCode.Congestion: return TransportError.Congestion;
			case ErrorCode.InvalidReceive: return TransportError.InvalidReceive;
			case ErrorCode.InvalidSend: return TransportError.InvalidSend;
			case ErrorCode.ConnectionClosed: return TransportError.ConnectionClosed;
			case ErrorCode.Unexpected: return TransportError.Unexpected;
			default: throw new InvalidCastException($"KCP: missing error translation for {error}");
		}
	}


	
	
	public async static Task Main()
	{
		Console.Write("Write");
		Log.Error += Console.Write;
		Log.Info += Console.Write;
		Log.Warning += Console.Write;
		var kcpClient = new KcpClient(
			OnConnected, 
			(msg, channel) => OnData(msg,channel == 0 ? KcpChannel.Reliable : KcpChannel.Unreliable)
			,OnDisconnected,  (error, reason) => OnError(ToTransportError(error), reason));
		kcpClient.Connect("127.0.0.1", 7777, true, 10, 2, false, 4096, 4096, 10000, Kcp.DEADLINK * 2, true);
		bool running = true;
		
		string? UserInput;

		Thread newWindowThread = new Thread(new ThreadStart(async delegate
		{
			await Update(kcpClient);
		}));
		
		newWindowThread.SetApartmentState(ApartmentState.STA);
		newWindowThread.IsBackground = true;
		newWindowThread.Start();
		while (running)
		{
			Console.WriteLine("Commands:");
			Console.WriteLine("1) StopServer");

			UserInput = Console.ReadLine();
			if (UserInput != null)
			{
				switch (UserInput)
				{
					case "1":
						var writer = new NetworkWriter();
						Pack(new Msg(), writer);
						var segment = writer.ToArraySegment();
						var butcher = new NetworkWriter();
						butcher.WriteDouble(100);
						butcher.WriteBytes(segment.Array, segment.Offset, segment.Count);
						kcpClient.Send(butcher.ToArraySegment() ,KcpChannel.Reliable);
						break;
					default:
						break;
				}
			}
			
			await Task.Delay(100);
		}
	}
	private async static Task Update(KcpClient kcpClient)
	{
		while (true)
		{
			kcpClient.Tick();
			await Task.Delay(100);	
		}
	}
	
	public static void Pack(Msg message, NetworkWriter writer)
	{
		writer.WriteUShort(GetId<Msg>());
		writer.WriteInt(message.id);
	}
	
	public static ushort GetId<T>() where T : struct, NetworkMessage =>
		(ushort)(typeof(T).FullName.GetStableHashCode());

	private static void OnError(TransportError toTransportError, string reason)
	{
		Console.Write($"Error is {reason}");
	}
	
	private static void OnDisconnected()
	{
		Console.Write($"OnDisconnected");
	}

	private static void OnData(ArraySegment<byte> arg1, KcpChannel arg2)
	{
		Console.Write($"OnData");

	}
	
	private static void OnConnected()
	{
		Console.Write($"OnConnected");
	}
}


public struct Msg : NetworkMessage
{
	public int id = 10;
	public Msg()
	{
	}
}