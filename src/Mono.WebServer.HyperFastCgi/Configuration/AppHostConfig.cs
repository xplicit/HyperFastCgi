using System;
using System.Xml.Serialization;
using HyperFastCgi.Logging;

namespace HyperFastCgi.Configuration
{
	[XmlRoot("apphost")]
	public class AppHostConfig
	{
		[XmlAttribute("type")]
		public string Type { get; set;}

		[XmlElement("loglevel")]
		public LogLevel LogLevel { get; set;}

		[XmlElement("add-trailing-slash")]
		public bool AddTrailingSlash { get; set;} 

	}
}

