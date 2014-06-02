using System;

namespace HyperFastCgi.Configuration
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

