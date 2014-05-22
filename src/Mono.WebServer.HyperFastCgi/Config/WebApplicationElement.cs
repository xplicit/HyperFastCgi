using System;
using System.Configuration;

namespace Mono.WebServer.HyperFastCgi.Config
{
	public class WebApplicationElement : ConfigurationElement
	{
		public string Name {get { return this["name"].ToString();}}

		public string VHost { get { return this["vhost"].ToString();}}

		public int VPort { get { return Convert.ToInt16(this["vport"]); }}

		public string VPath { get { return this["vpath"].ToString(); }}

		public string Path { get { return this["path"].ToString(); }}
	}
}

