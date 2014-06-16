using System;
using System.Xml.Serialization;

namespace HyperFastCgi.Configuration
{
	[Serializable]
	[XmlRoot("server")]
	public class AppServerConfig
	{
		[XmlAttribute("type")]
		public string Type { get; set;}

		[XmlElement("root-dir")]
		public string PhysycalRoot { get; set; }

		[XmlElement("host-factory")]
		public string HostFactoryType { get; set; }

		[XmlElement("threads")]
		public ThreadPoolConfig ThreadsConfig { get; set; }
	}
}

