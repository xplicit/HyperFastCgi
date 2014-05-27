using System;
using System.Xml.Serialization;
using Mono.WebServer.HyperFastCgi.Logging;

namespace Mono.WebServer.HyperFastCgi.Configuration
{
	[Serializable]
	[XmlRoot("log")]
	public class LogConfig
	{
		[XmlAttribute("level")]
		public LogLevel Level { get; set; }

		[XmlAttribute("write-to-console")]
		public bool WriteToConsole { get; set; }
	}
}

