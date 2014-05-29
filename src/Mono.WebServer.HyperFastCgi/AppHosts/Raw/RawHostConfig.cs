using System;
using System.Xml.Serialization;
using Mono.WebServer.HyperFastCgi.Configuration;

namespace Mono.WebServer.HyperFastCgi.AppHosts.Raw
{
	[Serializable]
	[XmlRoot("apphost")]
	public class RawHostConfig
	{
		[XmlAttribute("type")]
		public string Type { get; set;}

		[XmlElement("log")]
		public LogConfig Log { get; set;}

		[XmlElement("request-type")]
		public string RequestType {get; set;}
	}

}

