using System;

namespace Mono.WebServer.HyperFastCgi.Interfaces
{
	public interface INativeTransport
	{
		void CreateRequest (long requestId, int requestNumber);
		void AddServerVariable (long requestId, int requestNumber, string name, string value);
		void AddHeader (long requestId, int requestNumber, string name, string value);
		void HeadersSent (long requestId, int requestNumber);
		void AddBodyPart (long requestId, int requestNumber, byte[] body, bool final);
		void Process (long requestId, int requestNumber);

		void RegisterHost (string virtualPath,string path);
	}
}

