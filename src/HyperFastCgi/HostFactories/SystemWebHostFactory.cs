using System;
using HyperFastCgi.Interfaces;

namespace HyperFastCgi.HostFactories
{
	public class SystemWebHostFactory : IApplicationHostFactory
	{
		#region IApplicationHostFactory implementation
		public IApplicationHost CreateApplicationHost (Type appHostType, string vhost, int vport, string vpath, string path)
		{
			IApplicationHost host = System.Web.Hosting.ApplicationHost.CreateApplicationHost (appHostType, vpath, path) 
				as IApplicationHost;

			return host;
		}
		#endregion
	}
}

