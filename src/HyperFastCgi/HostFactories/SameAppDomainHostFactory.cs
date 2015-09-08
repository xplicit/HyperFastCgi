using System;
using HyperFastCgi.Helpers.Logging;
using HyperFastCgi.Interfaces;

namespace HyperFastCgi.HostFactories
{
	public class SameAppDomainHostFactory : IApplicationHostFactory
	{
		public IApplicationHost CreateApplicationHost(Type appHostType, string vhost, int vport, string vpath, string path)
		{
			return Activator.CreateInstance(appHostType) as IApplicationHost;
		}
	}
}
