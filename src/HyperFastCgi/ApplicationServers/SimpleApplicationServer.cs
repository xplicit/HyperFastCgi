using System;
using HyperFastCgi.Interfaces;
using HyperFastCgi.HostFactories;
using HyperFastCgi.Interfaces.Events;
using HyperFastCgi.Configuration;
using HyperFastCgi.Helpers.Logging;
using System.Collections.Generic;
using HyperFastCgi.Transports;
using HyperFastCgi.Helpers;
using System.Globalization;

namespace HyperFastCgi.ApplicationServers
{
	public class SimpleApplicationServer : MarshalByRefObject, IApplicationServer
	{
		private string physicalRoot;
		private List<HostInfo> hosts = new List<HostInfo> ();
		private AppServerConfig config;
		IApplicationHostFactory hostFactory;

		public string PhysicalRoot {
			get { return physicalRoot; }
		}

		public void Configure (object serverConfig)
		{
			config = serverConfig as AppServerConfig;

			physicalRoot = Environment.CurrentDirectory;

			if (config != null) {
				if (!String.IsNullOrEmpty (config.PhysycalRoot))
					physicalRoot = config.PhysycalRoot; 

				if (config.ThreadsConfig != null) {
					ThreadHelper.SetThreads (
						config.ThreadsConfig.MinWorkerThreads,
						config.ThreadsConfig.MinIOThreads,
						config.ThreadsConfig.MaxWorkerThreads,
						config.ThreadsConfig.MaxIOThreads
					);
				}

				if (!String.IsNullOrEmpty (config.HostFactoryType)) {
					Type factoryType = Type.GetType (config.HostFactoryType);
					if (factoryType == null) {
						Logger.Write (LogLevel.Error, "Could not find factory type '{0}'", config.HostFactoryType);
						return;
					}
					hostFactory = (IApplicationHostFactory)Activator.CreateInstance (factoryType);
				} else {
					hostFactory = new SystemWebHostFactory ();
				}
			}

			Logger.Write (LogLevel.Debug, "Root directory: {0}", physicalRoot);
		}

		public IApplicationHost GetRoute(string vhost, int vport, string vpath)
		{
			//TODO: read-write lock or ToArray() and lock-free.
			lock (hosts) {
				if (hosts.Count == 1)
					return hosts [0].Host;
				else {
					foreach (HostInfo host in hosts) {
						if (Match (host.Host, vhost, vport, vpath)) {
							return host.Host;
						}
					}
				}
			}

			return null;
		}

		private bool Match (IApplicationHost apphost, string vhost, int vport, string vpath)
		{
			if (vport != -1 && apphost.VPort != -1 && vport != apphost.VPort)
				return false;

//			if (vhost != null && apphost.VHost != null && apphost.VHost != "*") {
//				int length = apphost.VHost.Length;
//				string lwrvhost = vhost.ToLower (CultureInfo.InvariantCulture);
//				if (haveWildcard) {
//					if (length > vhost.Length)
//						return false;
//
//					if (length == vhost.Length && apphost.VHost != lwrvhost)
//						return false;
//
//					if (vhost [vhost.Length - length - 1] != '.')
//						return false;
//
//					if (!lwrvhost.EndsWith (apphost.VHost))
//						return false;
//
//				} else if (apphost.VHost != lwrvhost) {
//					return false;
//				}
//			}

			int local = vpath.Length;
			int vlength = apphost.VPath.Length;
			if (vlength > local) {
				// Check for /xxx requests to be redirected to /xxx/
				if (apphost.VPath [vlength - 1] != '/')
					return false;

				return (vlength - 1 == local && apphost.VPath.Substring (0, vlength - 1) == vpath);
			}

			return (vpath.StartsWith (apphost.VPath, StringComparison.Ordinal));
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
				IApplicationHost host = hostFactory.CreateApplicationHost (appHostType, appConfig.VHost, appConfig.VPort, appConfig.VPath, appConfig.RealPath);
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
	}
}

