using NetworkTester.Log;
using System.Net;
using System.Net.Sockets;

namespace NetworkTester.Manager
{
	public class UdpManager
	{
		/// <summary>
		/// UDP Server. Broadcasts datagrams to <paramref name="broadcastIpAddress"/> and <paramref name="broadcastPort"/> of <paramref name="bufferSize"/> bytes.
		/// </summary>
		/// <param name="broadcastIpAddress">IP address to broadcast data to.</param>
		/// <param name="broadcastPort">Port to broadcast data to.</param>
		/// <param name="logger">Logger to write log messages.</param>
		/// <param name="cancellationToken">Token to cancel operation.</param>
		/// <param name="bufferSize">Optional. Size of datagrams to broadcast. Default is <see cref="Data.DEFAULT_UDP_BUFFER_SIZE"/>.</param>
		/// <param name="writeRate">Optional. The rate in hertz of writing. Default is <see cref="Data.DEFAULT_WRITE_RATE"/></param>
		/// <exception cref="ArgumentException"><paramref name="bufferSize"/> is less than one.-or-<paramref name="writeRate"/> is zero.</exception>
		public UdpManager(IPAddress broadcastIpAddress, ushort broadcastPort, ILogger logger, CancellationToken cancellationToken, int bufferSize = Data.DEFAULT_UDP_BUFFER_SIZE, byte writeRate = Data.DEFAULT_WRITE_RATE)
		{
			if (bufferSize < 1)
			{
				throw new ArgumentException("Must be greater than zero", nameof(bufferSize));
			}
			if (writeRate < 1)
			{
				throw new ArgumentException("Must be greater than zero", nameof(writeRate));
			}

			StartWritingAsync(broadcastIpAddress, broadcastPort, bufferSize, writeRate, logger, cancellationToken);
		}

		/// <summary>
		/// UDP Client. Instantiates a <see cref="UdpClient"/> that reads data broadcast to <paramref name="port"/>.
		/// </summary>
		/// <param name="port">Port to read data from.</param>
		/// <param name="logger">Logger to write log messages.</param>
		/// <param name="cancellationToken">Token to cancel operation.</param>
		public UdpManager(ushort port, ILogger logger, CancellationToken cancellationToken)
		{
			StartReadingAsync(port, logger, cancellationToken);
		}

		private static async void StartWritingAsync(IPAddress broadcastIpAddress, ushort broadcastPort, int bufferSize, byte writeRate, ILogger logger, CancellationToken cancellationToken)
		{
			UdpClient? udpClient = null;
			try
			{
				using (udpClient = new())
				{
					udpClient.Connect(broadcastIpAddress, broadcastPort);

					Random random = new();
					byte[] datagram = new byte[bufferSize];
					while (!cancellationToken.IsCancellationRequested)
					{
						random.NextBytes(datagram);
						int bytesSent = await udpClient.SendAsync(datagram, datagram.Length).ConfigureAwait(false);
						logger.Log($"Wrote {bytesSent} bytes");
						await Task.Delay(Data.MILLISECONDS_IN_ONE_SECOND / writeRate, cancellationToken).ConfigureAwait(false);
					}
				}
			}
			catch (ObjectDisposedException ex)
			{
				logger.Log($"UdpClient {udpClient?.Client?.RemoteEndPoint} disposed. {ex}");
			}
			catch (SocketException ex)
			{
				logger.Log($"UdpClient {udpClient?.Client?.RemoteEndPoint} failed to create or write. WinSock Error {ex.ErrorCode}. {ex}");
			}
		}

		private static async void StartReadingAsync(ushort port, ILogger logger, CancellationToken cancellationToken)
		{
			UdpClient? client = null;
			try
			{
				using (client = new(port))
				{
					while (!cancellationToken.IsCancellationRequested)
					{
						UdpReceiveResult data = await client.ReceiveAsync(cancellationToken);
						logger.Log($"Read {data.Buffer.Length} bytes");
					}
				}
			}
			catch (ObjectDisposedException ex)
			{
				logger.Log($"UdpClient {client?.Client?.LocalEndPoint} disposed. {ex}");
			}
			catch (SocketException ex)
			{
				logger.Log($"Failed to bind or read from UdpClient {client?.Client?.LocalEndPoint}. WinSock Error {ex.ErrorCode}. {ex}");
			}
		}
	}
}
