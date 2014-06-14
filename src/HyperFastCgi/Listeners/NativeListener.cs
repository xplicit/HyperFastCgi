using System;
using System.Runtime.InteropServices;
using HyperFastCgi.Interfaces;
using HyperFastCgi.Configuration;
using HyperFastCgi.Transports;
using HyperFastCgi.Logging;

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

			return NativeListener.Listen ((ushort)config.Family, config.Address, (ushort)config.Port);
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

