using System;

namespace HyperFastCgi.Interfaces
{
	public interface IApplicationServer
	{
		string PhysicalRoot { get;}

		IApplicationHost GetRoute(string path);

		IApplicationHost CreateApplicationHost(
			Type appHostType, object appHostConfig, 
			string vhost, int vport, string vpath, string path, 
			IListenerTransport listenerTransport, Type transport, object transportConfig);
	}
}

