using System;
using System.Runtime.InteropServices;
using Mono.WebServer.HyperFastCgi.Interfaces;

namespace Mono.WebServer.HyperFastCgi.Listener
{
	public class NativeListener : IWebListener
	{
		#region IWebListener implementation

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
			get;
			set;
		}

		#endregion

		[DllImport("libhfc-native", EntryPoint="Listen")]
		public extern static int Listen(ushort family, string addr, ushort port);
	}
}

