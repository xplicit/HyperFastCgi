using System;
using System.Runtime.InteropServices;

namespace Mono.WebServer.HyperFastCgi.Listener
{
	public class NativeListener
	{
		[DllImport("libnative", EntryPoint="main")]
		public extern static int Listen(int argc, string[] argv);
	}
}

