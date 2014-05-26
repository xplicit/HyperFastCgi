using System;
using System.Xml.Serialization;

namespace Mono.WebServer.HyperFastCgi.Config
{
	[XmlRoot("apphost-transport")]
	public class TransportConfig
	{
		[XmlAttribute("type")]
		public string Type { get; set; }

	}
}

