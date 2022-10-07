using NetworkTester.Log;
using static NetworkTester.Manager.Data;

namespace NetworkTester
{
	public class Program
	{
		private const string INVALID_CALL = "Invalid call";
		private const string TCP_CLIENT_CALL = "TCP Client <ip address> <port> [buffer size]";
		private const string FOR_PROPER_USAGE = "For proper usage pass ?, -?, or /?";

		public static void Main(string[] args)
		{
			try
			{
				if (args.Length == 0 || args.Length == 2)
				{
					Console.WriteLine($"{INVALID_CALL}. {FOR_PROPER_USAGE}");
					return;
				}
				if (args.Length == 1)
				{
					ShowProperUsage(args[0]);
				}

				ILogger logger = new ConsoleLogger();

				if (!TryParseProtocol(args[0], out Protocol protocol))
				{
					logger.Log($"Failed to parse protocol {args[0]}");
					return;
				}
				if (!TryParseApplication(args[1], out Application application))
				{
					logger.Log($"Failed to parse application {args[1]}");
					return;
				}

				StartManager(protocol, application, args, logger);
			}
			finally
			{
				Console.WriteLine("Press any key to exit ...");
				Console.ReadKey();
			}
		}

		private static void ShowProperUsage(string requestUsage)
		{
			if (string.IsNullOrWhiteSpace(requestUsage) ||
						!requestUsage.Equals("?", StringComparison.OrdinalIgnoreCase) &&
						!requestUsage.Equals("-?", StringComparison.OrdinalIgnoreCase) &&
						!requestUsage.Equals("/?", StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine($"{INVALID_CALL}. {FOR_PROPER_USAGE}");
				return;
			}
			Console.WriteLine();
			Console.WriteLine("*********************************************************************************");
			Console.WriteLine("    Usage: (<> required parameters, [] optional paramters)");
			Console.WriteLine("    NetworkTester TCP Server <listening port> [buffer size] [write rate]");
			Console.WriteLine($"    NetworkTester {TCP_CLIENT_CALL}");
			Console.WriteLine("    NetworkTester UDP Server <ip address> <port> [buffer size] [write rate]");
			Console.WriteLine("    NetworkTester UDP Client <listening port> [buffer size]");
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

		private static void StartManager(Protocol protocol, Application application, string[] args, ILogger logger)
		{
			switch (protocol)
			{
				case Protocol.TCP:
					StartTcpManager(application, args, logger);
					break;
				case Protocol.UDP:
					break;
				default:
					logger.Log($"Invalid protocol {protocol}");
					break;
			}
		}

		private static void StartTcpManager(Application application, string[] args, ILogger logger)
		{
			switch (application)
			{
				case Application.Client:
					if (args.Length < 3)
					{
						logger.Log($"{INVALID_CALL}. {TCP_CLIENT_CALL}");
						return;
					}
					if (string.IsNullOrWhiteSpace(args[2]) || !ushort.TryParse(args[2], out ushort listeningPort))
					{
						logger.Log($"TCP Client invalid listening port. {TCP_CLIENT_CALL}");
						return;
					}
					int bufferSize;
					if (args.Length == 4 )
					{
						if (string.IsNullOrWhiteSpace(args[3]) ||
							!int.TryParse(args[3], out bufferSize) ||
							bufferSize < 1)
						{
							logger.Log($"TCP client invalid buffer size. {TCP_CLIENT_CALL}");
							return;
						}
					}
					break;
				case Application.Server:
					break;
				default:
					logger.Log($"Invalid application {application}");
					break;
			}
		}
	}
}