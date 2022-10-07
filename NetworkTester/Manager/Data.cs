namespace NetworkTester.Manager
{
	public static class Data
	{
		/// <summary>
		/// Application type.
		/// </summary>
		public enum Application
		{
			Unknown,
			Client,
			Server
		}

		/// <summary>
		/// Protocol type.
		/// </summary>
		public enum Protocol
		{
			Unknown,
			TCP,
			UDP
		}

		/// <summary>
		/// Default buffer size in bytes.
		/// </summary>
		public const ushort DEFAULT_BUFFER_SIZE = 1024;

		/// <summary>
		/// Default rate in hertz messages are written.
		/// </summary>
		/// <remarks>Hertz is the number of times per second the message is written. The default rate is to send
		/// a message four times a second so every 250ms.</remarks>
		public const byte DEFAULT_WRITE_RATE = 4;

		/// <summary>
		/// Number of milliseconds in a second.
		/// </summary>
		public const int MILLISECONDS_IN_ONE_SECOND = 1000;
	}
}
