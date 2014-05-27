using System;

namespace Mono.WebServer.HyperFastCgi.Configuration
{
	public class ConfigInfo
	{
		/// <summary>
		/// Type of the object we read config for
		/// </summary>
		/// <value>The object type.</value>
		public Type Type { get; set; }

		/// <summary>
		/// Config for the object we read config for 
		/// </summary>
		/// <value>The config.</value>
		/// <remarks>To deserialize XML to config, mark the class of 'Type' type 
		/// with [Config(YourConfigType)] attribute.</remarks>
		public object Config { get; set; }

		//extensinons for listener XML config
		public ConfigInfo ListenerTransport { get; set; }

		public ConfigInfo AppHostTransport { get; set; }
	}
}

