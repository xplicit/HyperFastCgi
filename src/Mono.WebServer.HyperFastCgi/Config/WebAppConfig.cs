using System;

namespace Mono.WebServer.HyperFastCgi.Config
{
	public class WebAppConfig
	{
		public string VHost { get; set;}

		public int VPort { get; set;}

		public string VPath { get; set;}

		public string RealPath { get; set;}
	}
}

