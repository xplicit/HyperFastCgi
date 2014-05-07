//
// NetworkConnector.cs: Processes fastcgi requests.
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
using System.Net.Sockets;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Net;
using Mono.WebServer.HyperFastCgi.FastCgiProtocol;
using Mono.WebServer.HyperFastCgi.Logging;
using Mono.WebServer.HyperFastCgi.Transport;

namespace Mono.WebServer.HyperFastCgi.Listener
{
	public enum ReadState
	{
		Header,
		Body,
		Padding
	}

	public class SendStateObject
	{
		public static int refCount = 0;
		public static volatile int lastTimeInfo = 0;
		public bool disconnectAfterSend;
		public byte[] buffer;
		public int offset;
		public Socket workSocket;
	}

	public class StateObject
	{
		//	public int offset;
		public byte[] buffer = new byte[16384];
		//read length prefix in this buffer
		public const int headerLength = 8;
		public byte[] header = new byte[headerLength];
		public int arrayOffset;
		public ReadState State = ReadState.Header;
		public Socket workSocket;
		public Record record = new Record ();
	}

	public class FastCgiNetworkConnector : IDisposable
	{
		private AsyncCallback asyncRecieveCallback;
		private AsyncCallback asyncSendCallback;
		private Socket client;
		private volatile bool isDisconnected;

		private Queue<Record> sendQueue = new Queue<Record> ();
		private SendStateObject sendState = new SendStateObject ();
		private int sendProcessing = 0;
		//all data from front-end were received. No need to receive more
		bool stopReceive;
		//front-end tells that all data have been sent and asks for shutdown socket one way
		bool readShutdown;
		bool keepAlive = true;
		bool useThreadPool = true;
		static int threadName = 0;
		private static int nConnect = 0;
		#pragma warning disable 414
		uint cn = 0;
		#pragma warning restore

		public bool UseThreadPool {
			get { return useThreadPool; }
			set { useThreadPool = value; }
		}

		public bool KeepAlive {
			get { return keepAlive; }
			set { keepAlive = value; }
		}

		public FastCgiListenerTransport Transport {
			get;
			set;
		}

		public uint Tag {
			get { return cn;}
		}


		public FastCgiNetworkConnector ()
		{
			asyncRecieveCallback = new AsyncCallback (ReceiveCallback);
			asyncSendCallback = new AsyncCallback (SendCallback);
			if (Thread.CurrentThread.Name [0] != 't') {
				Interlocked.Increment (ref threadName);
				Thread.CurrentThread.Name = "t" + threadName.ToString ();
			}
			Interlocked.Increment (ref nConnect);
			cn = (uint)nConnect;
		}

		public FastCgiNetworkConnector (Socket client) : this ()
		{
			this.client = client;
		}

		public FastCgiNetworkConnector (Socket client, ManagedFastCgiListener listener) : this (client)
		{
			this.Transport = new FastCgiListenerTransport (){Listener=listener};
		}

		public void Receive ()
		{
			try {
				StateObject state = new StateObject ();
				state.workSocket = client;

				// Begin receiving the data from the remote device.
				client.BeginReceive (state.buffer, 0, state.buffer.Length, 0, asyncRecieveCallback, state);
			} catch (Exception ex) {
				Logger.Write (LogLevel.Error, "Unhandled exception in Receive {0}", ex);
				throw;
			}

		}

		private void ReceiveCallback (IAsyncResult ar)
		{
			int offset = 0;
			int bytesRead = 0;
			StateObject state = (StateObject)ar.AsyncState;

			try {
				Socket client = state.workSocket;
				//	state.offset = 0;
				SocketError socketError;

				bytesRead = client.EndReceive (ar, out socketError);

				if (socketError != SocketError.Success) {
					//					log.DebugFormat("Socket error during receving [{0}]", socketError);
					OnDisconnected ();
					return;
				}

				// BytesRead==0 means, that socket is gracefully shutdown
				// on the other side. By the way it only means, that socket is shutdown
				// for sending data and it can wait for receiveing data. Underlying 
				// Connection can still be in ESTABLISHED state, even if socket is closed
				// (in case when socket reuses connection)
				if (bytesRead <= 0) {

					//Front-end disconnected
					readShutdown=true;
					return;
				}


				if (bytesRead > 0) {
					offset = 0;

					while (offset < bytesRead /*&& !stopReceive*/) {
						if (state.State == ReadState.Header) {
							int len;
							if ((len = StateObject.headerLength - state.arrayOffset) <= bytesRead - offset) {
								//TODO: change BlockCopy to "goto" statement
								Buffer.BlockCopy (state.buffer, offset, state.header, state.arrayOffset, len);
								offset += len;
								state.arrayOffset = 0;
								state.record.Version = state.header [0];
								state.record.Type = (RecordType)state.header [1];
								state.record.RequestId = (ushort)((state.header [2] << 8) + state.header [3]);
								state.record.BodyLength = (ushort)((state.header [4] << 8) + state.header [5]);
								state.record.PaddingLength = state.header [6];
								state.record.Body = new byte[state.record.BodyLength];
								//skip reserved field header[7]
								state.State = ReadState.Body;
								if (state.record.BodyLength == 0) {
									state.State = state.record.PaddingLength != 0 ? ReadState.Padding : ReadState.Header;
									if (state.State == ReadState.Header) {
										ProcessRecord (state.header,state.record.Body);
									}
								}
							} else {
								Buffer.BlockCopy (state.buffer, offset, state.header, state.arrayOffset, bytesRead - offset);
								state.arrayOffset += (bytesRead - offset);
								offset = bytesRead;
							}

						} else if (state.State == ReadState.Body) {
							int len;
							if ((len = state.record.BodyLength - state.arrayOffset) <= bytesRead - offset) {
								Buffer.BlockCopy (state.buffer, offset, state.record.Body, state.arrayOffset, len);
								offset += len;
								state.arrayOffset = 0;

								if (state.record.PaddingLength == 0) {
									state.State = ReadState.Header;
									ProcessRecord (state.header, state.record.Body);
								} else {
									state.State = ReadState.Padding;
								}
							} else {
								Buffer.BlockCopy (state.buffer, offset, state.record.Body, state.arrayOffset, bytesRead - offset);
								state.arrayOffset += (bytesRead - offset);
								offset = bytesRead;
							}
						} else if (state.State == ReadState.Padding) {
							if (state.record.PaddingLength - state.arrayOffset <= bytesRead - offset) {
								offset += state.record.PaddingLength - state.arrayOffset;
								state.State = ReadState.Header;
								//Process Record
								ProcessRecord (state.header, state.record.Body);
							} else {
								state.arrayOffset += bytesRead - offset;
								offset = bytesRead;
							}
						} else {
							//something wrong with packet
							Logger.Write (LogLevel.Error, "Wrong packet from HTTP server"); 
						}
					}
				}
				if (keepAlive || !stopReceive)
					client.BeginReceive (state.buffer, 0, state.buffer.Length, 0, asyncRecieveCallback, state);
			} catch (SocketException ex) {
				Logger.Write (LogLevel.Error, "ReceiveCallback. Socket error while receivieng data: {0}", ex.Message);
				OnDisconnected ();
			} catch (ObjectDisposedException) {
				Logger.Write (LogLevel.Error, "ReceiveCallback. Socket was closed");
				OnDisconnected ();
			} catch (Exception ex) {
				Logger.Write (LogLevel.Error, "Unhandled exception in recv {0}", ex);
				throw;
			}
		}

		public void ProcessRecord (byte[] header, byte[] body)
		{
			Logger.Write (LogLevel.Debug, "cn={0} read header={1} reqId={2}", cn, header [1], (ushort)((header [2] << 8) + header [3]));
			Transport.ProcessRecord (Tag, header, body);
		}

		private void StartSendPackets ()
		{
			if (isDisconnected) {
				Interlocked.Exchange (ref sendProcessing, 0);
				return;
			}

			bool hasPacket = false;
			Record packet = default(Record);
			try {
				//we have no items in queue except one we added, so send it 
				lock (sendQueue) {
					if (sendQueue.Count > 0) {
						packet = sendQueue.Dequeue ();
						hasPacket = true;
					}
				}

				Logger.Write (LogLevel.Debug, "cn={0} write header={1}, reqId={2}", cn, packet.Type, packet.RequestId);

				if (hasPacket) {
					if (packet.Type == RecordType.EndRequest || packet.Type == RecordType.AbortRequest) {
						sendState.disconnectAfterSend = true;
					}
					//TODO: change GetRecord() to GetHeader() and GetBody(byte[] arr,int offset, int length). 
					//This will increase performance for large packets, 
					//cause we don't need to use BlockCopy and create new byte[] in GetRecord()
					sendState.buffer = packet.GetRecord ();
					sendState.offset = 0;
					sendState.workSocket = this.client;
				}
			} catch (Exception ex) {
				Interlocked.Exchange (ref sendProcessing, 0);
				Logger.Write (LogLevel.Error, "Unhandled exception in StartSendPacket {0}", ex);
				throw;
			}

			try {
				if (!isDisconnected && hasPacket) {
					client.BeginSend (sendState.buffer, sendState.offset, sendState.buffer.Length, SocketFlags.None, asyncSendCallback, sendState);
				} else {
					Interlocked.Exchange (ref sendProcessing, 0);
				}
			} catch (SocketException ex) {
				Logger.Write (LogLevel.Error, "Socket error while sending data: {0}", ex.Message);
				sendState.buffer = null;
				sendState.workSocket = null;
				OnDisconnected ();
				Interlocked.Exchange (ref sendProcessing, 0);
			} catch (ObjectDisposedException ex) {
				Logger.Write (LogLevel.Error, "Socket error while sending data: {0}", ex.Message);
				sendState.buffer = null;
				sendState.workSocket = null;
				OnDisconnected ();
				Interlocked.Exchange (ref sendProcessing, 0);
			} catch (Exception ex) {
				Logger.Write (LogLevel.Error, "Unhandled exception in StartSendPacket 2  {0}", ex);
				sendState.buffer = null;
				sendState.workSocket = null;
				Interlocked.Exchange (ref sendProcessing, 0);
				throw;
			}
		}

		private void SendCallback (IAsyncResult ar)
		{
			SendStateObject sendState = (SendStateObject)ar.AsyncState;

			try {
				//Socket client = (Socket)ar.AsyncState;
				Socket client = sendState.workSocket;

				int bytesSent = client.EndSend (ar);

				if (bytesSent < sendState.buffer.Length - sendState.offset) {
					sendState.offset += bytesSent;
					client.BeginSend (sendState.buffer, sendState.offset, sendState.buffer.Length - sendState.offset, SocketFlags.None, asyncSendCallback, sendState);
					return;
				}

				if (sendState.disconnectAfterSend && !keepAlive) {
					Disconnect ();
				}
			} catch (SocketException ex) {
				Logger.Write (LogLevel.Error, "SendCallback. Socket error while sending data: {0}", ex.Message);
				OnDisconnected ();
				Interlocked.Exchange (ref sendProcessing, 0);
				return;
			} catch (ObjectDisposedException) {
				Logger.Write (LogLevel.Error, "SendCallback. Socket has already been disposed");
				OnDisconnected ();
				Interlocked.Exchange (ref sendProcessing, 0);
				return;
				//??? should we return or try to process other messages in queue.
			} catch (Exception ex) {
				Logger.Write (LogLevel.Error, "Unexpected exception in SendCallback", ex);
				Interlocked.Exchange (ref sendProcessing, 0);
				throw;
			} finally {
				//set sendState properties to null
				//workaround for fixing huge memory leak with sendstate
				if (sendState != null) {
					sendState.buffer = null;
					sendState.workSocket = null;
					sendState = null;
				}
			}

			bool continueSend = false; 

			lock (sendQueue) {
				if (sendQueue.Count > 0) {
					//get next element if we have
					continueSend = true;
				} else {
					Interlocked.Exchange (ref sendProcessing, 0);
				}
			}

			if (continueSend) {
				StartSendPackets ();
			}
		}

		public void SendRecord (Record record)
		{
			lock (sendQueue) {
				sendQueue.Enqueue (record);
			}

			//if we already have no other processes, then start to send  
			if (Interlocked.CompareExchange (ref sendProcessing, 1, 0) == 0) {
				StartSendPackets ();
			}

		}

		public void Disconnect ()
		{
			isDisconnected = true;

			//if (client.Connected)
			{
				client.Shutdown (SocketShutdown.Both);
				client.Disconnect (false);
				client.Close ();

			}
			OnDisconnected ();
		}

		#region Events

		public event EventHandler Connected;
		public event EventHandler Disconnected;

		#endregion

		protected void OnConnected ()
		{
			isDisconnected = false;
			EventHandler Connected = this.Connected;

			if (Connected != null) {
				Connected (this, new EventArgs ());
			}
		}

		protected void OnDisconnected ()
		{
			if (!isDisconnected) {
				isDisconnected = true;
				EventHandler Disconnected = this.Disconnected;

				if (Disconnected != null) {
					Disconnected (this, new EventArgs ());
				}
			}
		}

		#region IDisposable

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			//clear managed resources
			if (disposing) {
				client.Close ();
			}
		}

		~FastCgiNetworkConnector ()
		{
			Dispose (false);
		}

		#endregion

		public bool IsConnected {
			get { return !isDisconnected; }
		}
	}
}



