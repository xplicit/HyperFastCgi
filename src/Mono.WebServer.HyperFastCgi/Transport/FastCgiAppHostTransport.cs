using System;
using Mono.WebServer.HyperFastCgi.Interfaces;
using Mono.WebServer.HyperFastCgi.FastCgiProtocol;
using System.Collections.Generic;
using Mono.WebServer.HyperFastCgi.AspNetServer;
using System.Threading;
using Mono.WebServer.HyperFastCgi.Logging;

namespace Mono.WebServer.HyperFastCgi.Transport
{
	public class FastCgiAppHostTransport : MarshalByRefObject, IListenerTransport
	{
		private const uint maxRequestsCache=512;
		private Request[] requestsCache = new Request[maxRequestsCache];
		private Dictionary<uint,Request> requests = new Dictionary<uint, Request> ();
		private static object requestsLock = new object ();

		private Dictionary<uint,FastCgiRecordQueue> queues = new Dictionary<uint, FastCgiRecordQueue> ();
		private static object queuesLock = new object ();

		public IApplicationHost AppHost {
			get;
			set;
		}

		public FastCgiAppHostTransport ()
		{
		}

		public bool ProcessRecord (uint listenerTag, byte[] header,byte[] recordBody)
		{
			bool stopReceive = false;

			Record record = new Record ();
			record.Version = header [0];
			record.Type = (RecordType)header [1];
			record.RequestId = (ushort)((header [2] << 8) + header [3]);
			record.BodyLength = (ushort)((header [4] << 8) + header [5]);
			record.PaddingLength = header [6];
			record.Body = recordBody;

			FastCgiRecordQueue q = AddQueue (listenerTag, record);

			if (Interlocked.CompareExchange (ref q.processing, 1, 0) == 0) {
				//do process
				ThreadPool.QueueUserWorkItem (ProcessInternal, q);
			}

			return true;
		}

		public void ProcessInternal(object state)
		{
			FastCgiRecordQueue q = (FastCgiRecordQueue)state;
			uint listenerTag = q.listenerTag;

			Record record;
			bool stopReceive = false;

			while (q.queue.Count > 0) {
				Logger.Write (LogLevel.Debug, "lt={0} q.count={1}", listenerTag, q.queue.Count);

				lock (q.queueLock) {
					if (q.queue.Count > 0) {
						record = q.queue.Dequeue (); 
					} else {
						Interlocked.Exchange (ref q.processing, 0);
						return;
					}
				}
			

				//No multiplexing. If we need multiplexing we must construct
				//dictionary with key=<listenerTag,requestId>,value=<request>
				Request request = GetRequest (listenerTag);
				Logger.Write (LogLevel.Debug, "lt={0} ProcessInternal header={1} reqId={2} req={3}", listenerTag, record.Type, record.RequestId, 
					request == null ? "null" : request.RequestId.ToString());

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
						AddRequest (listenerTag, request);
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
							stopReceive = true;
							RemoveQueue (listenerTag);
							RemoveRequest (listenerTag);
							((FastCgiListenerTransport)AppHost.GetListenerTransport ()).RemoveRequest (listenerTag, request.RequestId);

							IWebRequest wreq = AppHost.CreateRequest (listenerTag, (int)listenerTag, null);
							wreq.Process (AppHost.GetResponse (wreq, null));

						}
					}
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
				//TODO: make Thread.Abort for request
				case RecordType.AbortRequest:
//				if (request != null) {
//					SendError (request.RequestId, Strings.Connection_AbortRecordReceived);
//					EndRequest (request.RequestId, -1, ProtocolStatus.RequestComplete);
//				}

					break;

				default:
//				SendRecord (new Record (Record.ProtocolVersion,
//					RecordType.UnknownType,
//					request.RequestId,
//					new UnknownTypeBody (record.Type).GetData ()));
					break;
				}
			}

			Interlocked.Exchange (ref q.processing, 0);
			return;
		}

		public void SendOutput (uint listenerTag, Request req, byte[] data, int length)
		{
			if (data == null)
				throw new ArgumentNullException ("data");

			if (data.Length == 0)
				return;

			if (req == null)
				return;

			req.StdOutSent = true;

			SendStreamData (listenerTag, RecordType.StandardOutput, req.RequestId, data, length);
		}

		private void SendStreamData (uint listenerTag, RecordType type, ushort requestId, byte[] data,
			int length)
		{
			// Records are only able to hold 65535 bytes of data. If
			// larger data is to be sent, it must be broken into
			// smaller components.

			if (length > data.Length)
				length = data.Length;

			if (length <= Record.MaxBodySize)
				SendRecord (listenerTag, type, requestId, data, 0, length);
			else {
				int index = 0;
				while (index < length) {
					int chunk_length = (length - index < Record.SuggestedBodySize) 
						? (length - index)
						: Record.SuggestedBodySize; 

					SendRecord (listenerTag, type, requestId,
						data, index, chunk_length);

					index += chunk_length;
				}
			}
		}

		public void SendRecord (uint listenerTag, RecordType type, ushort requestID,
			byte[] bodyData, int bodyIndex,
			int bodyLength)
		{
			Logger.Write (LogLevel.Debug, "at={0} SendRecord header={1} reqId={2}", listenerTag, type, requestID);

			byte[] header = new byte[Record.HeaderSize];
			header [0] = (byte)Record.ProtocolVersion;
			header [1] = (byte)type;
			header [2] = (byte)(requestID >> 8);
			header [3] = (byte)(requestID & 0xFF);
			header [4] = (byte)(bodyLength >> 8);
			header [5] = (byte)(bodyLength & 0xFF);
			header [6] = (byte)0;
			byte[] body = new byte[bodyLength];
			Buffer.BlockCopy (bodyData, bodyIndex, body, 0, bodyLength);

			((FastCgiListenerTransport)AppHost.GetListenerTransport()).SendRecord (listenerTag, header, body);
		}

		public void CompleteRequest (uint listenerTag, Request request, int appStatus)
		{
			CompleteRequest (listenerTag, request, appStatus, ProtocolStatus.RequestComplete);
		}

		private void CompleteRequest (uint listenerTag, Request request, int appStatus,
			ProtocolStatus protocolStatus)
		{
			if (request == null)
				return;

			// Close the standard output if it was opened.
			if (request.StdOutSent)
				SendStreamData (listenerTag, RecordType.StandardOutput, request.RequestId, new byte [0], 0);

			// Close the standard error if it was opened.
			if (request.StdErrSent)
				SendStreamData (listenerTag, RecordType.StandardError, request.RequestId, new byte [0], 0);

			EndRequest (listenerTag, request.RequestId, appStatus, protocolStatus);
		}

		public void EndRequest (uint listenerTag, ushort requestId, int appStatus,
			ProtocolStatus protocolStatus)
		{
			EndRequestBody body = new EndRequestBody (appStatus,
				protocolStatus);

			byte[] bodyData = body.GetData ();

			SendRecord (listenerTag, RecordType.EndRequest, requestId,
				bodyData, 0, bodyData.Length);
		}

		private FastCgiRecordQueue GetQueue(uint listenerTag)
		{
			FastCgiRecordQueue queue;

			lock (queuesLock) {
				queues.TryGetValue (listenerTag, out queue);
			}

			return queue;
		}

		private FastCgiRecordQueue AddQueue(uint listenerTag, Record record)
		{
			FastCgiRecordQueue q;

			lock (queuesLock) {
				if (!queues.TryGetValue (listenerTag, out q)) {
					q = new FastCgiRecordQueue ();
					q.listenerTag = listenerTag;
					queues.Add (listenerTag, q);
				} 
			}

			lock (q.queueLock) {
				q.queue.Enqueue (record);
			}

			return q;
		}

		private void RemoveQueue(uint listenerTag)
		{
			lock (queuesLock) {
				queues.Remove (listenerTag);
			}
		}

		private Request GetRequest (uint listenerTag)
		{
			Request request;

			if (listenerTag < maxRequestsCache) {
				request = requestsCache [(int)listenerTag];
			} else {
				lock (requestsLock) {
					requests.TryGetValue (listenerTag, out request);
				}
			}

			return request;
		}

		private void AddRequest (uint listenerTag, Request request)
		{
			if (listenerTag < maxRequestsCache) {
				requestsCache [(int)listenerTag] = request;
				//				Interlocked.Exchange (ref requestsCache [request.RequestId], request);
			} else {
				lock (requestsLock) {
					requests.Add (listenerTag, request);
				}
			}
		}

		private void RemoveRequest (uint listenerTag)
		{
			if (listenerTag < maxRequestsCache) {
				requestsCache [listenerTag] = null; 
				//				Interlocked.Exchange (ref requestsCache [requestId], null);
			} else {
				lock (requestsLock) {
					requests.Remove (listenerTag);
				}
			}
		}


	}
}

