using System;

namespace Mono.WebServer.HyperFastCgi.Config
{
	public class ConfigInfo
	{
		public Type Type { get; set; }

		public Type TransportType { get; set; }

		public object Config { get; set; }
	}
}

