using NetworkTester.Log;
using System.Net;
using System.Net.Sockets;

namespace NetworkTester.Manager
{
	/// <summary>
	/// Manages a <see cref="TcpListener"/>.
	/// </summary>
	public class TcpServerManager
	{
		/// <summary>
		/// Instantiates a <see cref="TcpListener"/> bound to <see cref="IPAddress.Any"/>:<paramref name="port"/> to listen for clients.
		/// </summary>
		/// <param name="port">Port to listen on.</param>
		/// <param name="logger">Logger to record messages.</param>
		/// <param name="cancellationToken">Token to cancel operation.</param>
		/// <param name="bufferSize">Optional. The size of the write buffer. Default is <see cref="Data.DEFAULT_BUFFER_SIZE"/></param>
		/// <param name="writeRate">Optional. The rate in hertz of writing. Default is <see cref="Data.DEFAULT_WRITE_RATE"/></param>
		/// <inheritdoc cref="TcpListener.Start()" select="Exception"/>
		/// <exception cref="ArgumentException"><paramref name="bufferSize"/> or <paramref name="writeRate"/> is zero.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="logger"/> is null.</exception>
		/// <remarks>Upon connection to a client the server begins writing <paramref name="bufferSize"/> random bytes at <paramref name="writeRate"/> in hertz.</remarks>
		public TcpServerManager(ushort port, ILogger logger, CancellationToken cancellationToken, int bufferSize = Data.DEFAULT_BUFFER_SIZE, byte writeRate = Data.DEFAULT_WRITE_RATE)
		{
			if (logger is null)
			{
				throw new ArgumentNullException(nameof(logger));
			}
			if (bufferSize < 1)
			{
				throw new ArgumentException("Must be greater than zero", nameof(bufferSize));
			}
			if (writeRate == 0)
			{
				throw new ArgumentException("Must be greater than zero", nameof(writeRate));
			}
			StartAsync(port, bufferSize, writeRate, logger, cancellationToken);
		}

		private static async void StartAsync(ushort port, int bufferSize, byte writeRate, ILogger logger, CancellationToken cancellationToken)
		{
			TcpListener server = new(IPAddress.Any, port);
			string serverIp = $"{IPAddress.Any}:{port}";
			try
			{
				server.Start();
				logger.Log($"Started server on {serverIp}");

				while (!cancellationToken.IsCancellationRequested)
				{
					logger.Log("Waiting for client ...");
					TcpClient client = await server.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
					logger.Log($"Accepted client {client.Client.RemoteEndPoint}");
					WriteAsync(client, bufferSize, writeRate, logger, cancellationToken);
				}
			}
			catch (SocketException ex)
			{
				logger.Log($"Socket exception thrown starting TCP Listener {serverIp}. WinSock Error: {ex.ErrorCode}. {ex}");
				return;
			}
			finally
			{
				try
				{
					server.Stop();
				}
				catch (SocketException ex)
				{
					logger.Log($"Socket exception stopping TCP Listener {serverIp}. WinSock Error: {ex.ErrorCode}. {ex}");
				}
			}
		}

		private static async void WriteAsync(TcpClient client, int bufferSize, byte writeRate, ILogger logger, CancellationToken cancellationToken)
		{
			if (!client.Connected)
			{
				return;
			}
			EndPoint? clientIp = client.Client.RemoteEndPoint;
			if (clientIp is null)
			{
				logger.Log($"Client disconnected.");
				return;
			}

			try
			{
				using (client)
				using (NetworkStream stream = client.GetStream())
				{
					if (!stream.CanWrite)
					{
						logger.Log($"Cannot write to client {client.Client.RemoteEndPoint}");
						return;
					}

					Random random = new();
					byte[] buffer = new byte[bufferSize];
					while (!cancellationToken.IsCancellationRequested)
					{
						random.NextBytes(buffer);
						try
						{
							await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
						}
						catch (IOException ex)
						{
							logger.Log($"IO Exception writing to TCP Client {clientIp}. {ex}");
							return;
						}
						logger.Log($"Wrote {bufferSize} bytes to client {clientIp}");
						Task.Delay(Data.MILLISECONDS_IN_ONE_SECOND / writeRate, cancellationToken).Wait(cancellationToken);
					}
				}
			}
			catch (ObjectDisposedException ex)
			{
				logger.Log($"TCP Client is closed. {ex}");
			}
		}
	}
}
