using System;
using Mono.WebServer.HyperFastCgi.Interfaces;
using Mono.WebServer.HyperFastCgi.Transport;
using Mono.WebServer.HyperFastCgi.Logging;
using Mono.WebServer.HyperFastCgi.Configuration;

namespace Mono.WebServer.HyperFastCgi.AspNetServer
{
	[Config(typeof(AspNetHostConfig))]
	public class AspNetApplicationHost : MarshalByRefObject, IApplicationHost
	{
		#region IApplicationHost implementation
		string path;
		string vpath;
		IListenerTransport listenerTransport;
		IApplicationHostTransport t;
		IApplicationServer appServer;

		public string Path {
			get {
				if (path == null)
					path = AppDomain.CurrentDomain.GetData (".appPath").ToString ();

				return path;
			}
		}

		public string VPath {
			get {
				if (vpath == null)
					vpath = AppDomain.CurrentDomain.GetData (".appVPath").ToString ();

				return vpath;
			}
		}

		public AppDomain Domain {
			get { return AppDomain.CurrentDomain; }
		}

		public IApplicationServer Server {
			get {return appServer;}
		}

		public IListenerTransport ListenerTransport {
			get { return listenerTransport; }
		}

		public IApplicationHostTransport AppHostTransport {
			get { return t; }
		}

		public IWebRequest CreateRequest (ulong requestId, int requestNumber, object arg)
		{
			return new AspNetNativeWebRequest (requestId, requestNumber, this, t, AddTrailingSlash);
		}

		public IWebResponse GetResponse (IWebRequest request, object arg)
		{
			return (IWebResponse)request;
		}

		public void ProcessRequest (IWebRequest request)
		{
			throw new NotImplementedException ();
		}

		public void Configure (IApplicationServer server, 
			IListenerTransport listenerTransport, 
			Type appHostTransportType, object transportConfig,
			object appHostConfig)
		{
			AspNetHostConfig config = appHostConfig as AspNetHostConfig;

			if (config != null) {
				LogLevel = config.Log.Level;
				LogToConsole = config.Log.WriteToConsole;
				AddTrailingSlash = config.AddTrailingSlash;
			}

			appServer = server;
			this.listenerTransport = listenerTransport;
			t = (IApplicationHostTransport) Activator.CreateInstance (appHostTransportType);
			t.Configure (this, transportConfig);
		}
		#endregion

		public LogLevel LogLevel {
			get { return Logger.Level; }
			set { Logger.Level = value; }
		}

		public bool LogToConsole {
			get { return Logger.WriteToConsole; }
			set { Logger.WriteToConsole = value; }
		}

		public bool AddTrailingSlash { get; set;}

	}
}

