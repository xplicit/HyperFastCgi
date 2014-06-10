using System;

namespace HyperFastCgi.Interfaces
{
	public interface IApplicationServer
	{
		string PhysicalRoot { get;}

		IApplicationHost GetRoute(string vhost, int vport, string vpath);

		IApplicationHost CreateApplicationHost(
			Type appHostType, object appHostConfig, 
			object webAppConfig,
			IListenerTransport listenerTransport, Type transport, object transportConfig);
	}
}

