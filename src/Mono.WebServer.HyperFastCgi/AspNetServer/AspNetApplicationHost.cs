using System;
using Mono.WebServer.HyperFastCgi.Interfaces;
using Mono.WebServer.HyperFastCgi.Transport;
using Mono.WebServer.HyperFastCgi.Logging;

namespace Mono.WebServer.HyperFastCgi.AspNetServer
{
	public class AspNetApplicationHost : MarshalByRefObject, IApplicationHost
	{
		#region IApplicationHost implementation
		string path;
		string vpath;
		IListenerTransport listenerTransport;
		INativeTransport t;
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

		public INativeTransport AppHostTransport {
			get { return t; }
		}

		public IWebRequest CreateRequest (ulong requestId, int requestNumber, object arg)
		{
			return new AspNetNativeWebRequest (requestId, requestNumber, this, t);
		}

		public IWebResponse GetResponse (IWebRequest request, object arg)
		{
			return (IWebResponse)request;
		}

		public void ProcessRequest (IWebRequest request)
		{
			throw new NotImplementedException ();
		}

		#endregion
		public AspNetApplicationHost()
		{
		}

		public void Configure (IApplicationServer server, IListenerTransport listenerTransport, Type transportType, object transportConfig)
		{
			appServer = server;
			this.listenerTransport = listenerTransport;
			t = (INativeTransport) Activator.CreateInstance (transportType);
			t.Configure (this, null);
		}

		public LogLevel LogLevel {
			get { return Logger.Level; }
			set { Logger.Level = value; }
		}

		public bool LogToConsole {
			get { return Logger.WriteToConsole; }
			set { Logger.WriteToConsole = value; }
		}

	}
}

