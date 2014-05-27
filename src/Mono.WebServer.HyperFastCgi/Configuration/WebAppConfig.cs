using System;
using System.Xml.Serialization;

namespace Mono.WebServer.HyperFastCgi.Configuration
{
	[XmlRoot("web-application")]
	public class WebAppConfig
	{
		private bool enabled = true;

		[XmlElement("name")]
		public string Name { get; set;}

		[XmlElement("vhost")]
		public string VHost { get; set;}

		[XmlElement("vport")]
		public int VPort { get; set;}

		[XmlElement("vpath")]
		public string VPath { get; set;}

		[XmlElement("path")]
		public string RealPath { get; set;}

		[XmlElement("enabled")]
		public bool Enabled { 
			get {return enabled; } 
			set {enabled = value; }
		}
	}
}

