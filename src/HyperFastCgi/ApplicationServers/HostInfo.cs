using System;
using HyperFastCgi.Configuration;
using HyperFastCgi.Interfaces;

namespace HyperFastCgi.ApplicationServers
{
	public class HostInfo
	{
		public IApplicationHost Host { get; set;}

		public Type AppHostType {get; set;}

		public object AppHostConfig { get; set;} 

		public WebAppConfig AppConfig { get; set; } 

		public IListenerTransport ListenerTransport { get; set; } 

		public Type AppHostTransportType {get; set;} 

		public object AppHostTransportConfig { get; set;}
	}
}

