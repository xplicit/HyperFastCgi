using System;

namespace Mono.WebServer.HyperFastCgi.Interfaces
{
	public interface IApplicationHost
	{
		string Path { get; }

		string VPath { get; }

		void ProcessRequest (IWebRequest request);

		IListenerTransport GetAppHostTransport();

		IListenerTransport GetListenerTransport (); 

		IApplicationServer Server { get; set;}
	}
}

