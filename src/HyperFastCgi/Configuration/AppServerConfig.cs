using System;
using System.Xml.Serialization;

namespace HyperFastCgi.Configuration
{
	[XmlRoot("server")]
	public class AppServerConfig
	{
		[XmlAttribute("type")]
		public string Type { get; set;}

	}
}

