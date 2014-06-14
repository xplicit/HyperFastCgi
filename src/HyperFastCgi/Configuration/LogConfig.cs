using System;
using System.Xml.Serialization;
using HyperFastCgi.Logging;

namespace HyperFastCgi.Configuration
{
	[Serializable]
	[XmlRoot("log")]
	public class LogConfig
	{
		private LogLevel level = LogLevel.Error;
		private bool writeToConsole = true;

		[XmlAttribute("level")]
		public LogLevel Level { 
			get { return level; } 
			set { level = value; }
		}

		[XmlAttribute("write-to-console")]
		public bool WriteToConsole { 
			get { return writeToConsole; } 
			set { writeToConsole = value; }
		}
	}
}

