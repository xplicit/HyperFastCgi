using System;
using System.Runtime.InteropServices;
using HyperFastCgi.Interfaces;
using HyperFastCgi.Configuration;
using HyperFastCgi.Transports;

namespace HyperFastCgi.Listeners
{
	[Config(typeof(ListenerConfig))]
	public class NativeListener : IWebListener
	{
		IApplicationServer server;

		#region IWebListener implementation

		public void Configure(object config, IApplicationServer server,
			Type listenerTransport, object listenerTransportConfig,
			Type appHostTransport, object appHostTransportConfig
		)
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

		public IApplicationServer Server {
			get { return server;}
		}

		public IListenerTransport Transport {
			get { return null; }
		}

		public Type AppHostTransportType {
			get { return typeof(NativeTransport); }
		}

		#endregion

		[DllImport("libhfc-native", EntryPoint="Listen")]
		private extern static int Listen(ushort family, string addr, ushort port);
	}
}

