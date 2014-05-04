using System;
using Mono.WebServer.HyperFastCgi.Interfaces;
using Mono.WebServer.HyperFastCgi.Sockets;
using System.Net.Sockets;
using System.Net;
using Mono.WebServer.HyperFastCgi.Logging;
using System.Collections.Generic;
using Mono.WebServer.HyperFastCgi.Transport;

namespace Mono.WebServer.HyperFastCgi.Listener
{
	public class ManagedFastCgiListener : IWebListener
	{
		bool keepAlive;
		bool useThreadPool;
		GeneralSocket listener;
		AsyncCallback accept;
		Dictionary <uint,FastCgiNetworkConnector> connectors = new Dictionary<uint, FastCgiNetworkConnector> ();
		object connectorsLock = new object ();

		#region IWebListener implementation

		public IListenerTransport Transport {
			get;
			set;
		}

		public IApplicationServer Server {
			get;
			set;
		}

		public void Listen (string host, int port)
		{
			this.keepAlive = true; //keepAlive;
			this.useThreadPool = true; //useThreadPool;

			try {
				this.listener = CreateSocket (GeneralSocketType.Tcp, host, port);

				listener.Listen (500);
				listener.BeginAccept (accept, listener);

			} catch (Exception ex) {
				Logger.Write (LogLevel.Error, "{0}", ex);
				return; //false;
			}
			Logger.Write (LogLevel.Debug, "Application started");

			return; //true;
		}

		public void Shutdown ()
		{
			throw new NotImplementedException ();
		}

		#endregion

		private GeneralSocket CreateSocket (GeneralSocketType sockType, string address, int port)
		{
			GeneralSocket socket = null;

			switch (sockType) {
			case GeneralSocketType.Unix:
				socket = new UnixSocket (address);
				break;
			case GeneralSocketType.Tcp:
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
				FastCgiNetworkConnector connector = new FastCgiNetworkConnector (client, (FastCgiListenerTransport)Transport);
				connector.KeepAlive = keepAlive;
				connector.UseThreadPool = useThreadPool;
				connector.Disconnected += OnDisconnect;
				lock (connectorsLock) {
					connectors.Add (connector.Tag, connector);
				}

				connector.Receive ();
			}
		}

		public ManagedFastCgiListener ()
		{
			accept = new AsyncCallback (acceptCallback);
			Transport = new FastCgiListenerTransport () {Listener=this};
		}

		public FastCgiNetworkConnector GetConnector(uint listenerTag)
		{
			FastCgiNetworkConnector connector = null;

			lock (connectorsLock) {
				connectors.TryGetValue (listenerTag,out connector);
			}

			return connector;
		}

		protected void OnDisconnect (object sender, EventArgs args)
		{
			FastCgiNetworkConnector connector = sender as FastCgiNetworkConnector;

			connector.Dispose ();
			lock (connectorsLock) {
				connectors.Remove (connector.Tag);
			}
		}

	}
}

