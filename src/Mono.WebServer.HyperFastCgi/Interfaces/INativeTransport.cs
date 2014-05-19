using System;

namespace Mono.WebServer.HyperFastCgi.Interfaces
{
	public interface INativeTransport
	{
		void Configure (IApplicationHost host, object config); 

		void CreateRequest (ulong requestId, int requestNumber);
		void AddServerVariable (ulong requestId, int requestNumber, string name, string value);
		void AddHeader (ulong requestId, int requestNumber, string name, string value);
		void HeadersSent (ulong requestId, int requestNumber);
		void AddBodyPart (ulong requestId, int requestNumber, byte[] body, bool final);
		void Process (ulong requestId, int requestNumber);

		void SendOutput (ulong requestId, int requestNumber, byte[] data, int len);
		void EndRequest (ulong requestId, int requestNumber, int appStatus);
	}
}

