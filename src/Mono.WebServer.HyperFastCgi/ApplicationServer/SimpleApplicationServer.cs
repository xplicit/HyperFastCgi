using System;
using Mono.WebServer.HyperFastCgi.Interfaces;
using Mono.WebServer.HyperFastCgi.AspNetServer;

namespace Mono.WebServer.HyperFastCgi.ApplicationServers
{
	public class SimpleApplicationServer : MarshalByRefObject, IApplicationServer
	{
		private IApplicationHost singleHost;
		private string physicalRoot;

		public string PhysicalRoot {
			get { return physicalRoot; }
		}

		public IApplicationHost GetRoute(string path)
		{
			return singleHost;
		}

		public IApplicationHost CreateApplicationHost(string vhost, int vport, string vpath, string path, 
			IListenerTransport listenerTransport, Type transportType, object transportConfig)
		{
			AspNetApplicationHostFactory factory = new AspNetApplicationHostFactory ();
			IApplicationHost host = factory.CreateApplicationHost (vhost, vport, vpath, path);
			host.Configure (this, listenerTransport, transportType, transportConfig);

			singleHost = host;

			return host;
		}

		public SimpleApplicationServer(string physicalRoot)
		{
			this.physicalRoot = physicalRoot;
		}
	}
}

