using NetworkTester.Log;
using System.Net;
using System.Net.Sockets;

namespace NetworkTester.Manager
{
	/// <summary>
	/// Manages a <see cref="TcpClient"/>.
	/// </summary>
	public class TcpClientManager
	{
		/// <summary>
		/// Instantiates a <see cref="TcpClient"/> that connects to <paramref name="ipAddress"/>:<paramref name="port"/>.
		/// </summary>
		/// <param name="ipAddress">IPAddress to connect to.</param>
		/// <param name="port">Port to connect to.</param>
		/// <param name="logger">Logger to record messages.</param>
		/// <param name="cancellationToken">Token to cancel operation.</param>
		/// <param name="bufferSize">Optional. The size of the read buffer. Default is <see cref="Data.DEFAULT_BUFFER_SIZE"/></param>
		/// <remarks>Upon connection the client will begin reading <paramref name="bufferSize"/> bytes.</remarks>
		/// <exception cref="ArgumentException"><paramref name="bufferSize"/> is zero or less.</exception>
		public TcpClientManager(IPAddress ipAddress, ushort port, ILogger logger, CancellationToken cancellationToken, int bufferSize = Data.DEFAULT_BUFFER_SIZE)
		{
			if (bufferSize < 1)
			{
				throw new ArgumentException($"Must be greater than zero", nameof(bufferSize));
			}
			StartAsync(ipAddress, port, bufferSize, logger, cancellationToken);
		}

		private static async void StartAsync(IPAddress ipAddress, ushort port, int bufferSize, ILogger logger, CancellationToken cancellationToken)
		{
			TcpClient client = new();
			string clientIp = $"{ipAddress}:{port}";
			try
			{
				await client.ConnectAsync(ipAddress, port, cancellationToken).ConfigureAwait(false);
			}
			catch (SocketException ex)
			{
				logger.Log($"Failed to connect {clientIp}. WinSock Error: {ex.ErrorCode}. {ex}");
				return;
			}
			logger.Log($"Client connected to {clientIp}. Will begin reading ...");
			ReadAsync(client, bufferSize, logger, cancellationToken);
		}

		private static async void ReadAsync(TcpClient client, int bufferSize, ILogger logger, CancellationToken cancellationToken)
		{
			EndPoint? clientIp = client.Client.RemoteEndPoint;
			if (clientIp is null)
			{
				logger.Log($"Remote endpoint is null");
				return;
			}
			if (!client.Connected)
			{
				logger.Log($"Client {clientIp} is not connected. Cannot read from it.");
				return;
			}

			try
			{
				using (client)
				using (NetworkStream stream = client.GetStream())
				{
					if (!stream.CanRead)
					{
						logger.Log($"Client {clientIp} stream is not readable.");
						return;
					}

					byte[] buffer = new byte[bufferSize];
					while (!cancellationToken.IsCancellationRequested)
					{
						try
						{
							int bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
							logger.Log($"Read {bytesRead} bytes");
							if (bytesRead == 0)
							{
								logger.Log($"Zero bytes read. Stream closed.");
								return;
							}
						}
						catch (IOException ex)
						{
							logger.Log($"Client {clientIp} failed to read. {ex}");
							return;
						}
					}
				}
			}
			catch (ObjectDisposedException ex)
			{
				logger.Log($"Client {clientIp} is closed. {ex}");
			}
		}
	}
}
