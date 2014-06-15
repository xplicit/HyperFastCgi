using System;
using System.Runtime.InteropServices;
using HyperFastCgi.Interfaces;
using HyperFastCgi.Configuration;
using HyperFastCgi.Transports;
using HyperFastCgi.Logging;
using System.Threading;

namespace HyperFastCgi.Listeners
{
	[Config(typeof(ListenerConfig))]
	public class NativeListener : IWebListener
	{
		IApplicationServer server;
		ListenerConfig config;

		#region IWebListener implementation

		public void Configure(object config, IApplicationServer server,
			Type listenerTransport, object listenerTransportConfig,
			Type appHostTransport, object appHostTransportConfig
		)
		{
			this.server = server;
			this.config = config as ListenerConfig;
		}

		public int Listen ()
		{
			Logger.Write (LogLevel.Debug,"Listening on port: {0}", config.Port);
			Logger.Write (LogLevel.Debug,"Listening on address: {0}", config.Address);

			int retval = NativeListener.Listen ((ushort)config.Family, config.Address, (ushort)config.Port);
			//retval == 0 when no error occured
			if (retval == 0) {
				ThreadPool.QueueUserWorkItem (_ => NativeListener.ProcessLoop ());
			}

			return retval;
		}

		public void Shutdown ()
		{
			NativeListener.InternalShutdown ();
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

		[DllImport("libhfc-native", EntryPoint="Shutdown")]
		private extern static void InternalShutdown();

		[DllImport("libhfc-native", EntryPoint="ProcessLoop")]
		private extern static void ProcessLoop();
	}
}

