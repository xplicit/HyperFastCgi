using System;

namespace Mono.WebServer.HyperFastCgi.Interfaces
{
	public interface IApplicationServer
	{
		string PhysicalRoot { get;}

		IApplicationHost GetRoute(string path);

		IApplicationHost CreateApplicationHost(string vhost, int vport, string vpath, string path, IListenerTransport transport);
	}
}

