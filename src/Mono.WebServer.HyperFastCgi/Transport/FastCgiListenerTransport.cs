using System;
using Mono.WebServer.HyperFastCgi.Interfaces;
using Mono.WebServer.HyperFastCgi.FastCgiProtocol;
using System.Collections.Generic;
using Mono.WebServer.HyperFastCgi.Listener;
using Mono.WebServer.HyperFastCgi.AspNetServer;
using System.Threading;
using Mono.WebServer.HyperFastCgi.Logging;

namespace Mono.WebServer.HyperFastCgi.Transport
{
	public class FastCgiListenerTransport : MarshalByRefObject, IListenerTransport
	{
		private const int maxRequestsCache=512;
		private TransportRequest[] requestsCache = new TransportRequest[maxRequestsCache];
		private Dictionary<ushort,TransportRequest> requests = new Dictionary<ushort, TransportRequest> ();
		private static object requestsLock = new object ();

		public ManagedFastCgiListener Listener {
			get;
			set;
		}

		public FastCgiListenerTransport ()
		{
		}

		/// <summary>
		/// Processes the record.
		/// </summary>
		/// <returns><c>true</c>, if record was sent, <c>false</c> otherwise.</returns>
		/// <param name="header">Header.</param>
		/// <param name="recordBody">Record body.</param>
		/// <remarks>Routes the record to proper ApplicationHost<remarks>
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
			Logger.Write(LogLevel.Debug, "lt={0} LT::ProcessRecord0 header={1} reqId={2}",listenerTag,
				header[1],(ushort)((header [2] << 8) + header [3]));

			TransportRequest request = GetRequest (record.RequestId);

			switch (record.Type) {
			case RecordType.BeginRequest:
				//TODO: check that body role is responder
				//add requestId to requests
//				if (body.Role == Role.Responder 
//					/*&& server.SupportsResponder*/) {
				AddRequest (new TransportRequest(record.RequestId,header,recordBody));
//				}
				break;
			case RecordType.Params:
				if (request != null) {
					//TODO: find application in the route
					//TODO: save route to the request
					//FIXME: can be two cases: route not found (no HOST param), need to wait next params  
					//or route not found (there is no matching HOST). Send error back in the case
					request.Transport=(FastCgiAppHostTransport)Listener.Server.GetRoute("test").GetAppHostTransport();
					//TODO: send BeginRequest, saved previously
					//FIXME: don't forget, that params can be in several FastCgi records
					//so we need to pass all saved data, not only begin requestbody
					if (request.Header != null) {
						Logger.Write(LogLevel.Debug, "lt={0} LT::ProcessRecord header={1} reqId={2}",listenerTag,
							request.Header[1],(ushort)((request.Header [2] << 8) + request.Header [3]));

						request.Transport.ProcessRecord (listenerTag, request.Header, request.Body);
						request.Header = null;
					}
					//send last Params request
					Logger.Write(LogLevel.Debug, "lt={0} LT::ProcessRecord header={1} reqId={2}",listenerTag,
						header[1],(ushort)((header [2] << 8) + header [3]));
					request.Transport.ProcessRecord (listenerTag, header, recordBody);
				} 
				break;
			case RecordType.StandardInput:
				//Ready to process
				if (request != null) {
					//TODO: ThreadPool.QueueUserWorkItem (we must not delay IO thread)
					//TODO: get routed host, send request to host as is
//					ThreadPool.QueueUserWorkItem ((state) => 
					Logger.Write(LogLevel.Debug, "lt={0} LT::ProcessRecord header={1} reqId={2}",listenerTag,
						header[1],(ushort)((header [2] << 8) + header [3]));
					request.Transport.ProcessRecord(listenerTag, header, recordBody);
//					);
				}
				//FIXME: wrong place of stopReceive? (when post data is large)
				stopReceive = true;
				break;
			case RecordType.Data:
				//TODO: ThreadPool.QueueUserWorkItem (we must not delay IO thread)
				//TODO: get routed host, send request to host as is
				break;
			case RecordType.GetValues:
				if (request != null) {
					//TODO: return server values
				}
				break;
				// Aborts a request when the server aborts.
				//TODO: make Thread.Abort for request
			case RecordType.AbortRequest:
				if (request != null) {
					//FIXME: send it to the HostTransport as is
					//TODO: send error to Connector
					//TODO: send EndRequest to Connector
//					SendError (request.RequestId, Strings.Connection_AbortRecordReceived);
//					EndRequest (request.RequestId, -1, ProtocolStatus.RequestComplete);
				}

				break;

			default:
				//TODO: CgiConnector.SendRecord
//				SendRecord (new Record (Record.ProtocolVersion,
//					RecordType.UnknownType,
//					request.RequestId,
//					new UnknownTypeBody (record.Type).GetData ()));
				break;
			}

			return stopReceive;
		}

		/// <summary>
		/// Sends the record to web listener
		/// </summary>
		/// <returns><c>true</c>, if record was sent, <c>false</c> otherwise.</returns>
		/// <param name="header">Header.</param>
		/// <param name="body">Body.</param>
		/// <description>This function sends record to web listener in listener domain</description>
		public bool SendRecord (uint listenerTag, byte[] header,byte[] body)
		{
			//get connector by it's tag
			Logger.Write (LogLevel.Debug, "lt={0} SendRecord", listenerTag); 
			FastCgiNetworkConnector connector = Listener.GetConnector (listenerTag);
			if (connector != null) {
				//TODO: optimize it 
				Record record = new Record ();
				record.Version = header [0];
				record.Type = (RecordType)header [1];
				record.RequestId = (ushort)((header [2] << 8) + header [3]);
				record.BodyLength = (ushort)((header [4] << 8) + header [5]);
				record.PaddingLength = header [6];
				record.Body = body;

				connector.SendRecord (record);
			} 

			return true;
		}

		public void RemoveRequest(uint listenerTag,ushort requestId)
		{
			Logger.Write (LogLevel.Debug, "lt={0} LT::RemoveRequest reqId={1}", listenerTag, requestId);
			FastCgiNetworkConnector connector = Listener.GetConnector (listenerTag);
			if (connector != null) {
				connector.Transport.RemoveRequest (requestId);
			}
		}

		#region requests caching and handling
		private TransportRequest GetRequest (ushort requestId)
		{
			TransportRequest request;

			if (requestId < maxRequestsCache) {
				request = requestsCache [requestId];
			} else {
				lock (requestsLock) {
					requests.TryGetValue (requestId, out request);
				}
			}

			return request;
		}

		private void AddRequest (TransportRequest request)
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

		private void RemoveRequest (ushort requestId)
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
		#endregion

	}
}

