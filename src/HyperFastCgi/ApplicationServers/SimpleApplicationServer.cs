using System;
using HyperFastCgi.Interfaces;
using HyperFastCgi.AppHosts.AspNet;
using HyperFastCgi.Interfaces.Events;
using HyperFastCgi.Configuration;
using HyperFastCgi.Logging;

namespace HyperFastCgi.ApplicationServers
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

		public IApplicationHost CreateApplicationHost(Type appHostType, object appHostConfig, 
			object webAppConfig, 
			IListenerTransport listenerTransport, Type transportType, object transportConfig)
		{
			WebAppConfig appConfig = webAppConfig as WebAppConfig;

			if (appConfig == null) {
				Logger.Write (LogLevel.Error, "Web application is not specified");
				return null;
			}

			AspNetApplicationHostFactory factory = new AspNetApplicationHostFactory ();
			IApplicationHost host = factory.CreateApplicationHost (appHostType, appConfig.VHost, appConfig.VPort, appConfig.VPath, appConfig.RealPath);
			host.HostUnload += (object sender, HostUnloadEventArgs e) => Console.WriteLine ("Host unload");
			host.Configure (appHostConfig, webAppConfig, this, listenerTransport, transportType, transportConfig);

			singleHost = host;

			return host;
		}

		public SimpleApplicationServer(string physicalRoot)
		{
			this.physicalRoot = physicalRoot;
		}
	}
}

