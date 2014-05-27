using System;

namespace Mono.WebServer.HyperFastCgi.Configuration
{
	public class ConfigAttribute : Attribute
	{
		public Type Type { get; set;}

		public ConfigAttribute (Type type)
		{
			this.Type = type;
		}
	}
}

