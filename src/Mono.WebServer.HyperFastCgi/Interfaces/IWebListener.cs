using System;
using System.Net.Sockets;

namespace Mono.WebServer.HyperFastCgi.Interfaces
{
	public interface IWebListener
	{
		IApplicationServer Server { get; }

		IListenerTransport Transport { get; }

		Type AppHostTransportType { get; }

		void Configure (IApplicationServer server, object config);

		int Listen (AddressFamily family, string host, int port);

		void Shutdown ();
	}
}

