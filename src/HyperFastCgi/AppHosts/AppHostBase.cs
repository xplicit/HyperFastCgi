using System;
using HyperFastCgi.Interfaces;
using HyperFastCgi.Interfaces.Events;
using HyperFastCgi.Configuration;
using HyperFastCgi.Helpers.Logging;

namespace HyperFastCgi.AppHosts
{
	public abstract class AppHostBase : MarshalByRefObject, IApplicationHost
	{
		#region IApplicationHost implementation
		string vhost;
		int vport;
		string vpath;
		string path;
		IListenerTransport listenerTransport;
		IApplicationHostTransport appHostTransport;
		IApplicationServer appServer;

		public virtual int VPort {
			get { return vport; }
		}

		public virtual string VHost {
			get { return vhost; }
		}

		public virtual string VPath {
			get {
				if (vpath == null)
					vpath = AppDomain.CurrentDomain.GetData (".appVPath").ToString ();

				return vpath;
			}
		}

		public virtual string Path {
			get {
				if (path == null)
					path = AppDomain.CurrentDomain.GetData (".appPath").ToString ();

				return path;
			}
		}

		public virtual AppDomain Domain {
			get { return AppDomain.CurrentDomain; }
		}

		public event EventHandler<HostUnloadEventArgs> HostUnload;

		public virtual IApplicationServer Server {
			get {return appServer;}
		}

		public virtual IListenerTransport ListenerTransport {
			get { return listenerTransport; }
		}

		public virtual IApplicationHostTransport AppHostTransport {
			get { return appHostTransport; }
		}

		public abstract IWebRequest CreateRequest (ulong requestId, int requestNumber, object arg);

		public abstract IWebResponse GetResponse (IWebRequest request, object arg);

		public abstract void ProcessRequest (IWebRequest request);

		public virtual void Configure (object appHostConfig, object webAppConfig, 
			IApplicationServer server, 
			IListenerTransport listenerTransport, 
			Type appHostTransportType, object transportConfig)
		{
			WebAppConfig appConfig = webAppConfig as WebAppConfig;
			if (appConfig != null) {
				vport = appConfig.VPort;
				vhost = appConfig.VHost;
				vpath = appConfig.VPath;
				path = appConfig.RealPath;
			}

			appServer = server;
			this.listenerTransport = listenerTransport;
			appHostTransport = (IApplicationHostTransport) Activator.CreateInstance (appHostTransportType);
			appHostTransport.Configure (this, transportConfig);
			Logger.Write (LogLevel.Debug, "Configured host in domain {0}, id={1}", AppDomain.CurrentDomain.FriendlyName, AppDomain.CurrentDomain.Id);
		}

		public virtual void Shutdown()
		{
			AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;

			EventHandler<HostUnloadEventArgs> handler = HostUnload;

			if (handler != null) {
				handler (this, new HostUnloadEventArgs (){ IsShutdown = true });
			}
		}

		public AppHostBase()
		{
			AppDomain.CurrentDomain.DomainUnload += new EventHandler (OnDomainUnload);
//			AppDomain.CurrentDomain.DomainUnload += (sender, e) => Console.WriteLine("Domain unload");
		}

		public virtual void OnDomainUnload (object sender, EventArgs e)
		{
			EventHandler<HostUnloadEventArgs> handler = HostUnload;

			if (handler != null) {
				handler (this, new HostUnloadEventArgs (){ IsShutdown = false });
			}
		}
		#endregion

	}

}

