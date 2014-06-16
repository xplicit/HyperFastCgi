using System;
using HyperFastCgi.Interfaces;
using HyperFastCgi.HostFactories;
using HyperFastCgi.Interfaces.Events;
using HyperFastCgi.Configuration;
using HyperFastCgi.Helpers.Logging;
using System.Collections.Generic;
using HyperFastCgi.Transports;

namespace HyperFastCgi.ApplicationServers
{
	public class SimpleApplicationServer : MarshalByRefObject, IApplicationServer
	{
		private string physicalRoot;
		private List<HostInfo> hosts = new List<HostInfo> ();

		public string PhysicalRoot {
			get { return physicalRoot; }
		}

		public IApplicationHost GetRoute(string vhost, int vport, string vpath)
		{
			lock (hosts) {
				if (hosts.Count > 0)
					return hosts [0].Host;
				else
					return null;
			}
		}

		public IApplicationHost CreateApplicationHost(Type appHostType, object appHostConfig, 
			object webAppConfig, 
			IListenerTransport listenerTransport, Type appHostTransportType, object appHostTransportConfig)
		{
			WebAppConfig appConfig = webAppConfig as WebAppConfig;

			if (appConfig == null) {
				Logger.Write (LogLevel.Error, "Web application is not specified");
				return null;
			}

			return CreateAppHost (appHostType, appHostConfig, appConfig, listenerTransport, appHostTransportType, appHostTransportConfig);
		}

		private IApplicationHost CreateAppHost(Type appHostType, object appHostConfig, 
			WebAppConfig appConfig, 
			IListenerTransport listenerTransport, Type appHostTransportType, object appHostTransportConfig)
		{
			try
			{
				SystemWebHostFactory factory = new SystemWebHostFactory ();
				IApplicationHost host = factory.CreateApplicationHost (appHostType, appConfig.VHost, appConfig.VPort, appConfig.VPath, appConfig.RealPath);
				host.Configure (appHostConfig, appConfig, this, listenerTransport, appHostTransportType, appHostTransportConfig);
				//subscribe to Unload event only after run host.Configure
				//because apphost transport must unregister himself first
				host.HostUnload += OnHostUnload;

				lock (hosts) {
					hosts.Add (new HostInfo () {
						Host = host,
						AppHostType = appHostType,
						AppHostConfig = appHostConfig,
						AppConfig = appConfig,
						ListenerTransport = listenerTransport,
						AppHostTransportType = appHostTransportType,
						AppHostTransportConfig = appHostTransportConfig
					});
				}
				return host;
			} catch (Exception ex) {
				Logger.Write (LogLevel.Error, "Can't create host {0}", ex);
				return null;
			}

		}

		private void OnHostUnload(object sender, HostUnloadEventArgs e)
		{
			HostInfo host = null;

			foreach (HostInfo info in hosts) {
				if (info.Host == sender) {
					host = info;
					break;
				}
			}

			if (host == null) {
				IApplicationHost appHost = sender as IApplicationHost;
				Logger.Write (LogLevel.Error, "Can't unload host {0}:{1}:{2}:{3}", appHost.VHost, appHost.VPort, appHost.VPath, appHost.Path);
				return;
			}

			Logger.Write (LogLevel.Debug, "Domain={0} Unload host in domain {1}", AppDomain.CurrentDomain.FriendlyName, ((IApplicationHost)sender).Domain.FriendlyName);

			lock (hosts) {
				hosts.Remove (host);
			}

			if (!e.IsShutdown) {
//				CombinedFastCgiListenerTransport.RegisterTransport (host.ListenerTransport, typeof(CombinedAppHostTransport));

				CreateAppHost (host.AppHostType, host.AppHostConfig, host.AppConfig,
					host.ListenerTransport, host.AppHostTransportType, host.AppHostTransportConfig);
			}
		}

		public SimpleApplicationServer(string physicalRoot)
		{
			this.physicalRoot = physicalRoot;
		}
	}
}

