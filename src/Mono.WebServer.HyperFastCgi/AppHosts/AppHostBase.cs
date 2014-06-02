using System;
using HyperFastCgi.Interfaces;

namespace HyperFastCgi.AppHosts
{
	public abstract class AppHostBase : MarshalByRefObject, IApplicationHost
	{
		#region IApplicationHost implementation
		string path;
		string vpath;
		IListenerTransport listenerTransport;
		IApplicationHostTransport appHostTransport;
		IApplicationServer appServer;

		public virtual string Path {
			get {
				if (path == null)
					path = AppDomain.CurrentDomain.GetData (".appPath").ToString ();

				return path;
			}
		}

		public virtual string VPath {
			get {
				if (vpath == null)
					vpath = AppDomain.CurrentDomain.GetData (".appVPath").ToString ();

				return vpath;
			}
		}

		public AppDomain Domain {
			get { return AppDomain.CurrentDomain; }
		}

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

		public virtual void Configure (IApplicationServer server, 
			IListenerTransport listenerTransport, 
			Type appHostTransportType, object transportConfig,
			object appHostConfig)
		{
			appServer = server;
			this.listenerTransport = listenerTransport;
			appHostTransport = (IApplicationHostTransport) Activator.CreateInstance (appHostTransportType);
			appHostTransport.Configure (this, transportConfig);
		}
		#endregion

	}

}

