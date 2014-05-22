using System;
using System.Net.Sockets;

namespace Mono.WebServer.HyperFastCgi.Interfaces
{
	public interface IWebListener
	{
		IListenerTransport Transport { get; set;}

		IApplicationServer Server { get; }

		void Configure (IApplicationServer server, object config);

		int Listen (AddressFamily family, string host, int port);

		void Shutdown ();
	}
}

