using System;
using System.Xml.Serialization;

namespace HyperFastCgi.Configuration
{
	[XmlRoot("threads")]
	public class ThreadPoolConfig
	{
		[XmlAttribute("min-worker")]
		public int MinWorkerThreads { get; set; }

		[XmlAttribute("max-worker")]
		public int MaxWorkerThreads { get; set; }

		[XmlAttribute("min-io")]
		public int MinIOThreads { get; set; }

		[XmlAttribute("max-io")]
		public int MaxIOThreads { get; set; }
	}
}

