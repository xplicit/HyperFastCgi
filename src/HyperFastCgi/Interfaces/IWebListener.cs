using System;
using System.Net.Sockets;

namespace HyperFastCgi.Interfaces
{
	public interface IWebListener
	{
		IApplicationServer Server { get; }

		IListenerTransport Transport { get; }

		Type AppHostTransportType { get; }

		void Configure (object listenerConfig, IApplicationServer server,
			Type listenerTransport, object listenerTransportConfig,
			Type appHostTransport, object appHostTransportConfig
		);

		int Listen (AddressFamily family, string host, int port);

		void Shutdown ();
	}
}

