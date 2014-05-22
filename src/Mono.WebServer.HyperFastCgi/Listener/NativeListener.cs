using System;
using System.Runtime.InteropServices;
using Mono.WebServer.HyperFastCgi.Interfaces;
using Mono.WebServer.HyperFastCgi.Config;

namespace Mono.WebServer.HyperFastCgi.Listener
{
	[Config(typeof(ListenerConfig))]
	public class NativeListener : IWebListener
	{
		IApplicationServer server;

		#region IWebListener implementation

		public void Configure(IApplicationServer server, object config)
		{
			this.server = server;
		}

		public int Listen (System.Net.Sockets.AddressFamily family, string host, int port)
		{
			return NativeListener.Listen ((ushort)family, host, (ushort)port);
		}

		public void Shutdown ()
		{
			throw new NotImplementedException ();
		}

		public IListenerTransport Transport {
			get;
			set;
		}

		public IApplicationServer Server {
			get { return server;}
		}

		#endregion

		[DllImport("libhfc-native", EntryPoint="Listen")]
		private extern static int Listen(ushort family, string addr, ushort port);
	}
}

