using System;
using HyperFastCgi.Interfaces;

namespace HyperFastCgi.HostFactories
{
	public class DifferentAppDomainHostFactory : IApplicationHostFactory
	{
		public IApplicationHost CreateApplicationHost(Type appHostType, string vhost, int vport, string vpath, string path)
		{
			var newAppDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString());

			return newAppDomain.CreateInstanceAndUnwrap(appHostType.Assembly.GetName().Name, appHostType.ToString())
				as IApplicationHost;
		}
	}
}

