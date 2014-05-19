using System;
using Mono.WebServer.HyperFastCgi.Interfaces;

namespace Mono.WebServer.HyperFastCgi.AspNetServer
{
	public class AspNetApplicationHostFactory : IApplicationHostFactory
	{
		#region IApplicationHostFactory implementation
		public IApplicationHost CreateApplicationHost (string vhost, int vport, string vpath, string path)
		{
			IApplicationHost host=System.Web.Hosting.ApplicationHost.CreateApplicationHost (typeof(AspNetApplicationHost), vpath, path) 
				as IApplicationHost;

			return host;
		}
		#endregion
	}
}

