//
// Server.cs: Listen on sockets and creates NetworkConnectors on accept
//
// Author:
//   Sergey Zhukov
//
// Copyright (C) 2013 Sergey Zhukov
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Mono.WebServer.HyperFastCgi.Logging;

namespace Mono.WebServer.HyperFastCgi
{
	public class Server
	{
		private ManualResetEvent allDone=new ManualResetEvent(false);
		private AsyncCallback accept;
		ApplicationHost appHost;
		GeneralSocket listener;
		bool keepAlive;
		bool useThreadPool;

		public Server ()
		{
			accept = new AsyncCallback (acceptCallback);
		}

		public Server(ApplicationHost appHost) : this ()
		{
			this.appHost = appHost;
		}

		public bool Start(GeneralSocketType sockType, string address, int port, bool keepAlive,bool useThreadPool)
		{
			this.keepAlive = keepAlive;
			this.useThreadPool = useThreadPool;
			//IPEndPoint localEP = new IPEndPoint(IPAddress.Any,port);

			//log.InfoFormat("Local address and port : {0}",localEP);

			try
			{
				this.listener = CreateSocket(sockType,address,port);
				//listener = new TcpSocket (localEP);
				//listener = new UnixSocket("/tmp/fastcgi.socket");

				listener.Listen(500);
				listener.BeginAccept(accept, listener);

			}
			catch (Exception ex) 
			{
				//log.Error(e);
				Logger.Write (LogLevel.Error, "{0}", ex);
				return false;
			}

			return true;
		}

		public void Shutdown()
		{
			listener.Close ();
			allDone.Set();

			//flush all changes
		}

		public GeneralSocket CreateSocket(GeneralSocketType sockType, string address, int port)
		{
			GeneralSocket socket = null;

			switch (sockType) {
			case GeneralSocketType.Unix:
				socket = new UnixSocket (address);
				break;
			case GeneralSocketType.Tcp:
				IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(address),port);
				socket = new TcpSocket (localEP);
				break;
			}

			return socket;
		}


		public void acceptCallback(IAsyncResult ar)
		{
			//TODO: add try/catch clause and raise EnexpectedException event 
			GeneralSocket listener = (GeneralSocket)ar.AsyncState;
			Socket client = listener.EndAccept(ar);

			//allDone.Set();
			listener.BeginAccept(accept, listener);


			// Additional code to read data goes here.
			NetworkConnector connector = new NetworkConnector(client,appHost);
			connector.KeepAlive = keepAlive;
			connector.UseThreadPool = useThreadPool;
			connector.Disconnected += OnDisconnect;

			connector.Receive();
		}

		protected void OnDisconnect(object sender, EventArgs args)
		{
			NetworkConnector connector = sender as NetworkConnector;

			//connector.Tag=null;
			connector.Dispose();

		}

	}
}

