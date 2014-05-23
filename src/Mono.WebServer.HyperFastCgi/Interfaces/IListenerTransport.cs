using System;

namespace Mono.WebServer.HyperFastCgi.Interfaces
{
	public interface IListenerTransport
	{
		void Configure (IWebListener listener, object config); 

		bool Process (ulong requestId, int requestNumber, byte[] header, byte[] body);

		void SendOutput (ulong requestId, int requestNumber, byte[] data, int len);
		void EndRequest (ulong requestId, int requestNumber, int appStatus);
	}
}

