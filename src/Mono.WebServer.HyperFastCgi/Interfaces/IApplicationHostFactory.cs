using System;

namespace Mono.WebServer.HyperFastCgi.Interfaces
{
	public interface IApplicationHostFactory
	{
		IApplicationHost CreateApplicationHost(Type appHostType, string vhost, int vport, string vpath, string path);
	}
}

