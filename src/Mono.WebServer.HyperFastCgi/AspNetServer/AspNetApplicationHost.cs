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
		IListenerTransport transport;
		NativeTransport t;

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


		public IListenerTransport GetAppHostTransport ()
		{
			if (transport == null) {
				//TODO: create instance
				transport = new FastCgiAppHostTransport(){AppHost=this};
			}

			return transport;
		}

		public IApplicationServer Server {
			get;
			set;
		}

		private IListenerTransport listenerTransport;

		public void SetListenerTransport(IListenerTransport transport)
		{
			listenerTransport = transport;
		}

		public IListenerTransport GetListenerTransport()
		{
			return listenerTransport;
		}

		public void ProcessRequest (IWebRequest request)
		{
			throw new NotImplementedException ();
		}


		#endregion
		public AspNetApplicationHost()
		{
			t = new NativeTransport ();
			t.AppHost = this;
			t.RegisterHost (VPath, Path);
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

