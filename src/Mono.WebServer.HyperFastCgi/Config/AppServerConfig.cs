using System;
using System.Xml.Serialization;

namespace Mono.WebServer.HyperFastCgi.Config
{
	[XmlRoot("server")]
	public class AppServerConfig
	{
		[XmlAttribute("type")]
		public string Type { get; set;}

	}
}

