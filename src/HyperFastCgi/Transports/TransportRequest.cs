using System;
using HyperFastCgi.Interfaces;
using System.Threading;
using System.Collections.Generic;

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

		public List<KeyValuePair> tempKeys = new List<KeyValuePair> (128);
		public string VHost;
		public int VPort = -1;
		public string VPath;

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

