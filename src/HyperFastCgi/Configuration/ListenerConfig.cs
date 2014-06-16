using System;
using System.Xml.Serialization;
using System.Net.Sockets;

namespace HyperFastCgi.Configuration
{
	[XmlRoot("listener")]
	public class ListenerConfig
	{
		[XmlAttribute("type")]
		public string Type { get; set;}

		[XmlElement("protocol")]
		public AddressFamily Family { get; set;}

		[XmlElement("address")]
		public string Address { get; set;}

		[XmlElement("port")]
		public int Port { get; set;}

	}
}

