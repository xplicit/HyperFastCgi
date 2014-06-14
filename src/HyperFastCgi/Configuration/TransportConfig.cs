using System;
using System.Xml.Serialization;

namespace HyperFastCgi.Configuration
{
	[Serializable]
	[XmlRoot("apphost-transport")]
	public class TransportConfig
	{
		[XmlAttribute("type")]
		public string Type { get; set; }

		[XmlElement("multithreading")]
		public MultiThreadingOption MultiThreading { get; set; }
	}
}

