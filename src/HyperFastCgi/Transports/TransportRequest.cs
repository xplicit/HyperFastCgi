using System;
using HyperFastCgi.Interfaces;
using System.Threading;

namespace HyperFastCgi.Transports
{
	public class TransportRequest
	{
		private static int nreq;

		public ulong Hash;
		public uint fd;
		public ushort RequestId;
		public int RequestNumber;
		public byte[] Header;
		public byte[] Body;
		public bool StdOutSent;
		public bool KeepAlive;


		//use 'host' for unmanaged transport
		public IntPtr Host;
		public IApplicationHostTransport Transport;

		public TransportRequest(ushort requestId, byte[] header, byte[] body)
		{
			this.RequestId = requestId;
			this.RequestNumber = Interlocked.Increment (ref nreq);
			this.Header = (byte [])header.Clone();
			this.Body = (byte[] )body.Clone();
		}
	}
}

