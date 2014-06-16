using System;

namespace HyperFastCgi.Interfaces
{
	public interface IApplicationServer
	{
		string PhysicalRoot { get;}

		void Configure (object config);

		IApplicationHost GetRoute(string vhost, int vport, string vpath);

		IApplicationHost CreateApplicationHost(
			Type appHostType, object appHostConfig, 
			object webAppConfig,
			IListenerTransport listenerTransport, Type transport, object transportConfig);
	}
}

