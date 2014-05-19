using System;

namespace Mono.WebServer.HyperFastCgi.Interfaces
{
	public interface IApplicationHostFactory
	{
		IApplicationHost CreateApplicationHost(string vhost, int vport, string vpath, string path);
	}
}

