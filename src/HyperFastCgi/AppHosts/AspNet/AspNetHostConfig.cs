using System;
using System.Xml.Serialization;
using HyperFastCgi.Configuration;

namespace HyperFastCgi.AppHosts.AspNet
{
	[Serializable]
	[XmlRoot("apphost")]
	public class AspNetHostConfig
	{
		[XmlAttribute("type")]
		public string Type { get; set;}

		[XmlElement("log")]
		public LogConfig Log { get; set;}

		[XmlElement("add-trailing-slash")]
		public bool AddTrailingSlash { get; set;} 
	}

}

