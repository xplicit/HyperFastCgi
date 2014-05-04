using System;

namespace Mono.WebServer.HyperFastCgi.Interfaces
{
	public interface IWebListener
	{
		IListenerTransport Transport { get; set;}

		IApplicationServer Server { get; set;}

		void Listen (string host, int port);

		void Shutdown ();
	}
}

