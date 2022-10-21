using NetworkTester.Log;
using NetworkTester.Manager;
using System.Net;
using static NetworkTester.Manager.Data;

namespace NetworkTester
{
	public class Program
	{
		private const string INVALID_CALL = "Invalid call";
		private const string TCP_SERVER_CALL = "TCP Server <listening port> [buffer size] [write rate]";
		private const string TCP_CLIENT_CALL = "TCP Client <ip address> <port> [buffer size]";
		private const string UDP_SERVER_CALL = "UDP Server <ip address> <port> [buffer size] [write rate]";
		private const string UDP_CLIENT_CALL = "UDP Client <listening port>";
		private const string FOR_PROPER_USAGE = "For proper usage pass ?, -?, or /?";

		public static void Main(string[] args)
		{
			CancellationTokenSource cancellationTokenSource = new();
			try
			{
				if (args.Length == 0 || args.Length == 2)
				{ // No arguments or only two is invalid
					Console.WriteLine($"{INVALID_CALL}. {FOR_PROPER_USAGE}");
					return;
				}
				if (args.Length == 1)
				{ // One argument should be a usage call
					ShowProperUsage(args[0]);
					return;
				}

				ILogger logger = new ConsoleLogger();

				if (!TryParseProtocol(args[0], out Protocol protocol))
				{ // First argument should be protocol: TCP or UDP
					logger.Log($"Failed to parse protocol {args[0]}");
					return;
				}
				if (!TryParseApplication(args[1], out Application application))
				{ // Second argument should be application: Client or Server 
					logger.Log($"Failed to parse application {args[1]}");
					return;
				}

				StartManager(args, protocol, application, logger, cancellationTokenSource.Token);
			}
			finally
			{
				Console.WriteLine("Press any key to exit ...");
				Console.ReadKey();

				cancellationTokenSource.Cancel();
				cancellationTokenSource.Dispose();
			}
		}

		private static void ShowProperUsage(string requestUsage)
		{
			if (string.IsNullOrWhiteSpace(requestUsage) || !requestUsage.Equals("?", StringComparison.OrdinalIgnoreCase) &&
				!requestUsage.Equals("-?", StringComparison.OrdinalIgnoreCase) && !requestUsage.Equals("/?", StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine($"{INVALID_CALL}. {FOR_PROPER_USAGE}");
				return;
			}
			Console.WriteLine();
			Console.WriteLine("*********************************************************************************");
			Console.WriteLine("    Usage: (<> required parameters, [] optional paramters)");
			Console.WriteLine($"    NetworkTester.exe {TCP_SERVER_CALL}");
			Console.WriteLine($"    NetworkTester.exe {TCP_CLIENT_CALL}");
			Console.WriteLine($"    NetworkTester.exe {UDP_SERVER_CALL}");
			Console.WriteLine($"    NetworkTester.exe {UDP_CLIENT_CALL}");
			Console.WriteLine("*********************************************************************************");
			Console.WriteLine();
		}

		private static bool TryParseProtocol(string protocolArg, out Protocol protocol)
		{
			protocol = Protocol.Unknown;
			if (string.IsNullOrWhiteSpace(protocolArg))
			{
				return false;
			}
			try
			{
				protocol = (Protocol)Enum.Parse(typeof(Protocol), protocolArg);
				return true;
			}
			catch (Exception ex) when (ex is ArgumentException || ex is OverflowException)
			{
				return false;
			}
		}

		private static bool TryParseApplication(string applicationArg, out Application application)
		{
			application = Application.Unknown;
			if (string.IsNullOrWhiteSpace(applicationArg))
			{
				return false;
			}
			try
			{
				application = (Application)Enum.Parse(typeof(Application), applicationArg);
				return true;
			}
			catch (Exception ex) when (ex is ArgumentException || ex is OverflowException)
			{
				return false;
			}
		}

		private static void StartManager(string[] args, Protocol protocol, Application application, ILogger logger, CancellationToken cancellationToken)
		{
			switch (protocol)
			{
				case Protocol.TCP:
					StartTcpManager(args, application, logger, cancellationToken);
					break;
				case Protocol.UDP:
					StartUdpManager(args, application, logger, cancellationToken);
					break;
				default:
					logger.Log($"Invalid protocol {protocol}");
					break;
			}
		}

		private static void StartTcpManager(string[] args, Application application, ILogger logger, CancellationToken cancellationToken)
		{
			switch (application)
			{
				case Application.Client:
					if (args.Length < 4)
					{ // TCP Client <ip address> <port> [buffer size]
						logger.Log($"{INVALID_CALL}. {TCP_CLIENT_CALL}");
						return;
					}
					if (string.IsNullOrWhiteSpace(args[2]) || !IPAddress.TryParse(args[2], out IPAddress? ipAddress))
					{ // IP Address
						logger.Log($"TCP Client invalid IP address {args[2]}. {TCP_CLIENT_CALL}");
						return;
					}
					if (string.IsNullOrWhiteSpace(args[3]) || !ushort.TryParse(args[3], out ushort port))
					{ // Port
						logger.Log($"TCP Client invalid port {args[3]}. {TCP_CLIENT_CALL}");
						return;
					}
					int bufferSize = DEFAULT_BUFFER_SIZE;
					if (args.Length == 5)
					{ // Buffer Size
						if (string.IsNullOrWhiteSpace(args[4]) || !int.TryParse(args[4], out bufferSize) || bufferSize < 1)
						{
							logger.Log($"TCP Client invalid buffer size {args[4]}. {TCP_CLIENT_CALL}");
							return;
						}
					}

					_ = new TcpClientManager(ipAddress, port, logger, cancellationToken, bufferSize);
					break;
				case Application.Server:
					if (args.Length < 3)
					{ // TCP Server <listening port> [buffer size] [write rate]
						logger.Log($"{INVALID_CALL}. {TCP_SERVER_CALL}");
						return;
					}
					if (string.IsNullOrWhiteSpace(args[2]) || !ushort.TryParse(args[2], out ushort listeningPort))
					{ // Listening Port
						logger.Log($"TCP Server invalid listening port {args[2]}. {TCP_SERVER_CALL}");
						return;
					}
					bufferSize = DEFAULT_BUFFER_SIZE;
					if (args.Length >= 4)
					{ // Buffer Size
						if (string.IsNullOrWhiteSpace(args[3]) || !int.TryParse(args[3], out bufferSize) || bufferSize < 1)
						{
							logger.Log($"TCP Server invalid buffer size {args[3]}. {TCP_SERVER_CALL}");
							return;
						}
					}
					byte writeRate = DEFAULT_WRITE_RATE;
					if (args.Length == 5)
					{ // Write Rate
						if (string.IsNullOrWhiteSpace(args[4]) || !byte.TryParse(args[4], out writeRate) || writeRate < 1)
						{
							logger.Log($"TCP Server invalid write rate {args[4]}. {TCP_SERVER_CALL}");
							return;
						}
					}

					_ = new TcpServerManager(listeningPort, logger, cancellationToken, bufferSize, writeRate);
					break;
				default:
					logger.Log($"Invalid TCP application: {application}");
					break;
			}
		}

		private static void StartUdpManager(string[] args, Application application, ILogger logger, CancellationToken cancellationToken)
		{
			switch (application)
			{
				case Application.Client:
					if (args.Length < 3)
					{ // UDP Client <listening port>
						logger.Log($"{INVALID_CALL}. {UDP_CLIENT_CALL}");
						return;
					}
					if (string.IsNullOrWhiteSpace(args[2]) || !ushort.TryParse(args[2], out ushort listeningPort))
					{ // Listening Port
						logger.Log($"UDP Client invalid port {args[2]}. {UDP_CLIENT_CALL}");
						return;
					}

					_ = new UdpManager(listeningPort, logger, cancellationToken);
					break;
				case Application.Server:
					if (args.Length < 4)
					{ // UDP Server <ip address> <port> [buffer size] [write rate]
						logger.Log($"{INVALID_CALL}. {UDP_SERVER_CALL}");
						return;
					}
					if (string.IsNullOrWhiteSpace(args[2]) || !IPAddress.TryParse(args[2], out IPAddress? ipAddress))
					{ // IP Address
						logger.Log($"UDP Server invalid IP address {args[2]}. {UDP_SERVER_CALL}");
						return;
					}
					if (string.IsNullOrWhiteSpace(args[3]) || !ushort.TryParse(args[3], out ushort broadcastPort))
					{ // Port
						logger.Log($"UDP Server invalid port {args[3]}. {UDP_SERVER_CALL}");
						return;
					}
					int bufferSize = DEFAULT_BUFFER_SIZE;
					if (args.Length > 4)
					{ // Buffer Size
						if (string.IsNullOrWhiteSpace(args[4]) || !int.TryParse(args[4], out bufferSize) || bufferSize < 1 || bufferSize > MAX_UDP_BUFFER_SIZE)
						{
							logger.Log($"UDP Server invalid buffer size {args[4]}. Must be 1-{MAX_UDP_BUFFER_SIZE}. {UDP_SERVER_CALL}");
							return;
						}
					}
					byte writeRate = DEFAULT_WRITE_RATE;
					if (args.Length == 6)
					{ // Write Rate
						if (string.IsNullOrWhiteSpace(args[5]) || !byte.TryParse(args[5], out writeRate) || writeRate < 1)
						{
							logger.Log($"UDP Server invalid write rate {args[5]}. {UDP_SERVER_CALL}");
							return;
						}
					}

					_ = new UdpManager(ipAddress, broadcastPort, logger, cancellationToken, bufferSize, writeRate);
					break;
				default:
					logger.Log($"Invalid UDP application: {application}");
					break;
			}
		}
	}
}