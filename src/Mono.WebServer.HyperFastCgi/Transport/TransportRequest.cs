using System;
using Mono.WebServer.HyperFastCgi.Interfaces;

namespace Mono.WebServer.HyperFastCgi.Transport
{
	public class TransportRequest
	{
		public ushort RequestId;
		public byte[] Header;
		public byte[] Body;
		public FastCgiAppHostTransport Transport;

		public TransportRequest()
		{
		}

		public TransportRequest(ushort requestId, byte[] header, byte[] body)
		{
			this.RequestId = requestId;
			this.Header = (byte [])header.Clone();
			this.Body = (byte[] )body.Clone();
		}
	}
}

