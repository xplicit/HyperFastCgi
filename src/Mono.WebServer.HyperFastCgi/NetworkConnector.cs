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

namespace Mono.WebServer.HyperFastCgi
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

	public class NetworkConnector : IDisposable
	{
		private WaitCallback processCallback;
		private AsyncCallback asyncRecieveCallback;
		private AsyncCallback asyncSendCallback;
		private Socket client;
		private volatile bool isDisconnected;

		private const int maxRequestsCache=512;
		private Request[] requestsCache = new Request[maxRequestsCache];
		private Dictionary<ushort,Request> requests = new Dictionary<ushort, Request> ();
		private static object requestsLock = new object ();

		private Queue<Record> sendQueue = new Queue<Record> ();
		private SendStateObject sendState = new SendStateObject ();
		private int sendProcessing = 0;
		//all data from front-end were received. No need to receive more
		bool stopReceive;
		//front-end tells that all data have been sent and asks for shutdown socket one way
		bool readShutdown;
		bool keepAlive = true;
		bool useThreadPool = true;
		private ApplicationHost appHost;
		static int threadName = 0;
		private static int nConnect = 0;
		#pragma warning disable 414
		int cn = 0;
		#pragma warning restore

		public bool UseThreadPool {
			get { return useThreadPool; }
			set { useThreadPool = value; }
		}

		public bool KeepAlive {
			get { return keepAlive; }
			set { keepAlive = value; }
		}

		public NetworkConnector ()
		{
			processCallback = new WaitCallback (ProcessInternal);
			asyncRecieveCallback = new AsyncCallback (ReceiveCallback);
			asyncSendCallback = new AsyncCallback (SendCallback);
			if (Thread.CurrentThread.Name [0] != 't') {
				Interlocked.Increment (ref threadName);
				Thread.CurrentThread.Name = "t" + threadName.ToString ();
			}
			Interlocked.Increment (ref nConnect);
			cn = nConnect;
		}

		public NetworkConnector (Socket client) : this ()
		{
			this.client = client;
		}

		public NetworkConnector (Socket client, ApplicationHost appHost) : this (client)
		{
			this.appHost = appHost;
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
										ProcessRecord (state.record);
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
									ProcessRecord (state.record);
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
								ProcessRecord (state.record);
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

		public void ProcessRecord (Record record)
		{
			Request request = GetRequest (record.RequestId);

			switch (record.Type) {
			case RecordType.BeginRequest:
				//CreateRequest
				BeginRequestBody body = new BeginRequestBody (record);

				if (request != null) {
					throw new ArgumentException ("Request is not served!");
					//EndRequest (record.RequestId, 200, ProtocolStatus.RequestComplete);
					//break;
				}
				// If the role is "Responder", and it is
				// supported, create a ResponderRequest.
				if (body.Role == Role.Responder 
					/*&& server.SupportsResponder*/) {				
					request = new Request (record.RequestId);
					AddRequest (request);
				}
				break;
			case RecordType.Params:
				if (request != null)
					request.AddParameterData (record.Body, true); 
				break;
			case RecordType.StandardInput:
				//Ready to process
				if (request != null) {
					if (request.AddInputData (record)) {
						if (useThreadPool)
							ThreadPool.QueueUserWorkItem (processCallback, request);
						else
							appHost.ProcessRequest (this, request);
					}
				}
				stopReceive = true;
				break;
			case RecordType.Data:
				if (request != null) {
					request.AddFileData (record);
				}
				break;
			case RecordType.GetValues:
				if (request != null) {
					//TODO: return server values
				}
				break;
			// Aborts a request when the server aborts.
			case RecordType.AbortRequest:
				if (request == null)
					break;

				SendError (request.RequestId, Strings.Connection_AbortRecordReceived);
				EndRequest (request.RequestId, -1, ProtocolStatus.RequestComplete);

				break;

			default:
				SendRecord (new Record (Record.ProtocolVersion,
					RecordType.UnknownType,
					request.RequestId,
					new UnknownTypeBody (record.Type).GetData ()));
				break;
			}
		}

		private void ProcessInternal (object state)
		{
			appHost.ProcessRequest (this, state as Request);
			//TestSend (record.RequestId);
			//EndRequest (record.RequestId, 0, ProtocolStatus.RequestComplete);
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

		public void EndRequest (ushort requestId, int appStatus,
		                        ProtocolStatus protocolStatus)
		{
			EndRequestBody body = new EndRequestBody (appStatus,
				                      protocolStatus);

			RemoveRequest (requestId);

			try {	
				if (!isDisconnected) {
					SendRecord (new Record (Record.ProtocolVersion, RecordType.EndRequest, requestId,
						body.GetData ()));
					if (readShutdown)
						OnDisconnected();
				}
			} catch (System.Net.Sockets.SocketException) {
				throw;
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

		public void SendRecord (RecordType type, ushort requestID,
		                        byte[] bodyData, int bodyIndex,
		                        int bodyLength)
		{
			Record record = new Record (Record.ProtocolVersion, type, requestID,
				               bodyData, bodyIndex,
				               bodyLength);

			SendRecord (record);
		}

		private void TestSend (ushort requestId)
		{
			byte[] toSend1 = Encoding.Default.GetBytes (TestResponse.Header);
			byte[] toSend2 = Encoding.Default.GetBytes (TestResponse.Response);

			SendStreamData (RecordType.StandardOutput, requestId, toSend1, toSend1.Length);
			SendStreamData (RecordType.StandardOutput, requestId, toSend2, toSend2.Length);
			SendRecord (RecordType.StandardOutput, requestId, new byte[0], 0, 0);
		}

		private void SendStreamData (RecordType type, ushort requestId, byte[] data,
		                             int length)
		{
			// Records are only able to hold 65535 bytes of data. If
			// larger data is to be sent, it must be broken into
			// smaller components.

			if (length > data.Length)
				length = data.Length;

			if (length <= Record.MaxBodySize)
				SendRecord (type, requestId, data, 0, length);
			else {
				int index = 0;
				while (index < length) {
					int chunk_length = (length - index < Record.SuggestedBodySize) 
						? (length - index)
						: Record.SuggestedBodySize; 

					SendRecord (type, requestId,
						data, index, chunk_length);

					index += chunk_length;
				}
			}
		}

		public void CompleteRequest (ushort requestId, int appStatus)
		{
			CompleteRequest (requestId, appStatus, ProtocolStatus.RequestComplete);
		}

		private void CompleteRequest (ushort requestId, int appStatus,
		                              ProtocolStatus protocolStatus)
		{
			// Data is no longer needed.
			//DataNeeded = false;
			Request req = GetRequest (requestId);

			// Close the standard output if it was opened.
			if (req.StdOutSent)
				SendStreamData (RecordType.StandardOutput, requestId, new byte [0], 0);

			// Close the standard error if it was opened.
			if (req.StdErrSent)
				SendStreamData (RecordType.StandardError, requestId, new byte [0], 0);

			EndRequest (requestId, appStatus,
				protocolStatus);
		}

		#region Standard Output Handling

		public void SendOutput (ushort requestId, byte[] data, int length)
		{
			if (data == null)
				throw new ArgumentNullException ("data");

			if (data.Length == 0)
				return;

			Request req = GetRequest (requestId);
			req.StdOutSent = true;

			SendStreamData (RecordType.StandardOutput, req.RequestId, data, length);
		}

		public void SendOutput (ushort requestId, byte[] data)
		{
			SendOutput (requestId, data, data.Length);
		}

		public void SendOutputText (ushort requestId, string text)
		{
			SendOutput (requestId, text, System.Text.Encoding.UTF8);
		}

		public void SendOutput (ushort requestId, string text, System.Text.Encoding encoding)
		{
			SendOutput (requestId, encoding.GetBytes (text));
		}

		#endregion

		#region Standard Error Handling

		public void SendError (ushort requestId, byte[] data, int length)
		{
			if (data == null)
				throw new ArgumentNullException ("data");

			if (data.Length == 0)
				return;

			Request req = GetRequest (requestId);
			req.StdErrSent = true;

			SendStreamData (RecordType.StandardError, requestId, data, length);
		}

		public void SendError (ushort requestId, byte[] data)
		{
			SendError (requestId, data, data.Length);
		}

		public void SendError (ushort requestId, string text)
		{
			SendError (requestId, text, System.Text.Encoding.UTF8);
		}

		public void SendError (ushort requestId, string text,
		                       System.Text.Encoding encoding)
		{
			SendError (requestId, encoding.GetBytes (text));
		}

		#endregion

		public Request GetRequest (ushort requestId)
		{
			Request request;

			if (requestId < maxRequestsCache) {
				request = requestsCache [requestId];
			} else {
				lock (requestsLock) {
					requests.TryGetValue (requestId, out request);
				}
			}

			return request;
		}

		public void AddRequest (Request request)
		{
			if (request.RequestId < maxRequestsCache) {
				requestsCache [request.RequestId] = request;
//				Interlocked.Exchange (ref requestsCache [request.RequestId], request);
			} else {
				lock (requestsLock) {
					requests.Add (request.RequestId, request);
				}
			}
		}

		public void RemoveRequest (ushort requestId)
		{
			if (requestId < maxRequestsCache) {
				requestsCache [requestId] = null; 
//				Interlocked.Exchange (ref requestsCache [requestId], null);
			} else {
				lock (requestsLock) {
					requests.Remove (requestId);
				}
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

		~NetworkConnector ()
		{
			Dispose (false);
		}

		#endregion

		public bool IsConnected {
			get { return !isDisconnected; }
		}
	}
}

