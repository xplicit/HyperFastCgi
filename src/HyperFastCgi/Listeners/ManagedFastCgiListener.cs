using System;
using HyperFastCgi.Interfaces;
using HyperFastCgi.Helpers.Sockets;
using System.Net.Sockets;
using System.Net;
using HyperFastCgi.Helpers.Logging;
using System.Collections.Generic;
using HyperFastCgi.Transports;
using HyperFastCgi.Configuration;

namespace HyperFastCgi.Listeners
{
	[Config(typeof(ListenerConfig))]
	public class ManagedFastCgiListener : IWebListener
	{
		GeneralSocket listener;
		AsyncCallback accept;

		IApplicationServer server;
		IListenerTransport transport;
		Type appHostTransportType;

		ListenerConfig config;

		#region IWebListener implementation

		public IApplicationServer Server {
			get { return server; }
		}

		public IListenerTransport Transport {
			get { return transport; }
		}

		public Type AppHostTransportType {
			get { return appHostTransportType; }
		}

		public void Configure(object conf, IApplicationServer server, 
			Type listenerTransport, object listenerTransportConfig,
			Type appHostTransport, object appHostTransportConfig)
		{
			this.server = server;
			this.config = conf as ListenerConfig;

			transport = (IListenerTransport)Activator.CreateInstance (listenerTransport);
			transport.Configure (this, listenerTransportConfig);

			appHostTransportType = appHostTransport;
		}

		public int Listen ()
		{
			try {
				Logger.Write (LogLevel.Debug,"Listening on port: {0}", config.Port);
				Logger.Write (LogLevel.Debug,"Listening on address: {0}", config.Address);

				this.listener = CreateSocket (config.Family, config.Address, config.Port);

				listener.Listen (500);
				listener.BeginAccept (accept, listener);

			} catch (Exception ex) {
				Logger.Write (LogLevel.Error, "{0}", ex);
				return 1; //false;
			}
			Logger.Write (LogLevel.Debug, "Application started");

			return 0; //true;
		}

		public void Shutdown ()
		{
			listener.Close ();
			FastCgiNetworkConnector.ShutdownAll ();
		}

		#endregion

		private GeneralSocket CreateSocket (AddressFamily sockType, string address, int port)
		{
			GeneralSocket socket = null;

			switch (sockType) {
			case AddressFamily.Unix:
				socket = new UnixSocket (address);
				break;
			case AddressFamily.InterNetwork:
				IPEndPoint localEP = new IPEndPoint (IPAddress.Parse (address), port);
				socket = new TcpSocket (localEP);
				break;
			}

			return socket;
		}

		public void acceptCallback (IAsyncResult ar)
		{
			GeneralSocket listener = (GeneralSocket)ar.AsyncState;
			Socket client=null;

			try {
				client = listener.EndAccept (ar);
			}
			catch (ObjectDisposedException) {
				//socket has been closed in Shutdown method
			}

			if (client != null) {
				listener.BeginAccept (accept, listener);

				// Additional code to read data goes here.
				FastCgiNetworkConnector connector = new FastCgiNetworkConnector (client, this);
				connector.Disconnected += OnDisconnect;

				connector.Receive ();
			}
		}

		public ManagedFastCgiListener ()
		{
			accept = new AsyncCallback (acceptCallback);
		}

		protected void OnDisconnect (object sender, EventArgs args)
		{
			FastCgiNetworkConnector connector = sender as FastCgiNetworkConnector;

			connector.Dispose ();
		}

	}
}

