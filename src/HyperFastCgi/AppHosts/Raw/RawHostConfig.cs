using System;
using System.Xml.Serialization;
using HyperFastCgi.Configuration;

namespace HyperFastCgi.AppHosts.Raw
{
	[Serializable]
	[XmlRoot("apphost")]
	public class RawHostConfig
	{
		private LogConfig log = new LogConfig();

		[XmlAttribute("type")]
		public string Type { get; set;}

		[XmlElement("log")]
		public LogConfig Log { 
			get { return log; } 
			set { log = value; }
		}

		[XmlElement("request-type")]
		public string RequestType {get; set;}
	}

}

