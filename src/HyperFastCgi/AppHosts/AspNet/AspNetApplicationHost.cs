using System;
using HyperFastCgi.Interfaces;
using HyperFastCgi.Transports;
using HyperFastCgi.Logging;
using HyperFastCgi.Configuration;

namespace HyperFastCgi.AppHosts.AspNet
{
	[Config(typeof(AspNetHostConfig))]
	public class AspNetApplicationHost : AppHostBase
	{
		#region IApplicationHost implementation

		public override IWebRequest CreateRequest (ulong requestId, int requestNumber, object arg)
		{
			return new AspNetNativeWebRequest (requestId, requestNumber, this, base.AppHostTransport, AddTrailingSlash);
		}

		public override IWebResponse GetResponse (IWebRequest request, object arg)
		{
			return (IWebResponse)request;
		}

		public override void ProcessRequest (IWebRequest request)
		{
			throw new NotImplementedException ();
		}

		public override void Configure (object appHostConfig, object webAppConfig, 
			IApplicationServer server, 
			IListenerTransport listenerTransport, 
			Type appHostTransportType, object transportConfig)
		{
			AspNetHostConfig config = appHostConfig as AspNetHostConfig;

			if (config != null) {
				LogLevel = config.Log.Level;
				LogToConsole = config.Log.WriteToConsole;
				AddTrailingSlash = config.AddTrailingSlash;
			}

			base.Configure (appHostConfig, webAppConfig, server, listenerTransport, appHostTransportType, transportConfig);
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

