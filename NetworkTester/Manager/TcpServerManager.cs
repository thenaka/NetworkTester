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
		/// <param name="logger">Logger to write log messages.</param>
		/// <param name="cancellationToken">Token to cancel operation.</param>
		/// <param name="bufferSize">Optional. The size of the write buffer. Default is <see cref="Data.DEFAULT_BUFFER_SIZE"/></param>
		/// <param name="writeRate">Optional. The rate in hertz of writing. Default is <see cref="Data.DEFAULT_WRITE_RATE"/></param>
		/// <inheritdoc cref="TcpListener.Start()" select="Exception"/>
		/// <exception cref="ArgumentException"><paramref name="bufferSize"/> or <paramref name="writeRate"/> is zero.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="logger"/> is null.</exception>
		/// <remarks>Upon connection to a client the server begins writing <paramref name="bufferSize"/> random bytes at <paramref name="writeRate"/> in hertz.</remarks>
		public TcpServerManager(ushort port, ILogger logger, CancellationToken cancellationToken, int bufferSize = Data.DEFAULT_BUFFER_SIZE, byte writeRate = Data.DEFAULT_WRITE_RATE)
		{
			if (bufferSize < 1)
			{
				throw new ArgumentException("Must be greater than zero", nameof(bufferSize));
			}
			if (writeRate < 1)
			{
				throw new ArgumentException("Must be greater than zero", nameof(writeRate));
			}
			StartAsync(port, bufferSize, writeRate, logger, cancellationToken);
		}

		private static async void StartAsync(ushort port, int bufferSize, byte writeRate, ILogger logger, CancellationToken cancellationToken)
		{
			TcpListener server = new(IPAddress.Any, port);
			EndPoint serverEndPoint = server.LocalEndpoint;
			try
			{
				server.Start();
				logger.Log($"Started server {serverEndPoint}");

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
				logger.Log($"Socket exception thrown starting TCP Listener {serverEndPoint}. WinSock Error: {ex.ErrorCode}. {ex}");
			}
			finally
			{
				try
				{
					server.Stop();
				}
				catch (SocketException ex)
				{
					logger.Log($"Socket exception stopping TCP Listener {serverEndPoint}. WinSock Error: {ex.ErrorCode}. {ex}");
				}
			}
		}

		private static async void WriteAsync(TcpClient client, int bufferSize, byte writeRate, ILogger logger, CancellationToken cancellationToken)
		{
			EndPoint? clientEndPoint = client.Client.RemoteEndPoint;
			if (clientEndPoint is null)
			{
				logger.Log($"Client is null. Cannot write.");
				return;
			}
			if (!client.Connected)
			{
				logger.Log($"Client {clientEndPoint} is not connected. Cannot write.");
				return;
			}

			try
			{
				using (client)
				using (NetworkStream stream = client.GetStream())
				{
					if (!stream.CanWrite)
					{
						logger.Log($"Cannot write to client {clientEndPoint}");
						return;
					}

					Random random = new();
					byte[] buffer = new byte[bufferSize];
					while (!cancellationToken.IsCancellationRequested)
					{
						random.NextBytes(buffer);
						await stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
						logger.Log($"Wrote {bufferSize} bytes to client {clientEndPoint}");
						await Task.Delay(Data.MILLISECONDS_IN_ONE_SECOND / writeRate, cancellationToken).ConfigureAwait(false);
					}
				}
			}
			catch (IOException ex)
			{
				logger.Log($"IO Exception writing to TCP Client {clientEndPoint}. {ex}");
			}
			catch (ObjectDisposedException ex)
			{
				logger.Log($"TCP Client is disposed. {ex}");
			}
		}
	}
}
