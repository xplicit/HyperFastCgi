using System;
using Mono.WebServer.HyperFastCgi.Interfaces;
using Mono.WebServer.HyperFastCgi.FastCgiProtocol;
using System.Collections.Generic;
using Mono.WebServer.HyperFastCgi.Listener;
using Mono.WebServer.HyperFastCgi.AppHosts.AspNet;
using System.Threading;
using Mono.WebServer.HyperFastCgi.Logging;

namespace Mono.WebServer.HyperFastCgi.Transport
{
	public class FastCgiListenerTransport : MarshalByRefObject, IListenerTransport
	{
		private Dictionary<ulong,TransportRequest> requests = new Dictionary<ulong, TransportRequest> ();
		private static object requestsLock = new object ();
		private IWebListener listener;
		bool debugEnabled = false;

		public IWebListener Listener {
			get { return listener; }
		}

		public FastCgiListenerTransport ()
		{
		}

		public void Configure (IWebListener listener, object config)
		{
			this.listener = listener;
		}

		/// <summary>
		/// Processes the record.
		/// </summary>
		/// <returns><c>true</c>, if record was sent, <c>false</c> otherwise.</returns>
		/// <param name="header">Header.</param>
		/// <param name="recordBody">Record body.</param>
		/// <remarks>Routes the record to proper ApplicationHost<remarks>
		public bool Process (ulong listenerTag, int num, byte[] header,byte[] recordBody)
		{
			bool stopReceive = false;

			Record record = new Record ();
			record.Version = header [0];
			record.Type = (RecordType)header [1];
			record.RequestId = (ushort)((header [2] << 8) + header [3]);
			record.BodyLength = (ushort)((header [4] << 8) + header [5]);
			record.PaddingLength = header [6];
			record.Body = recordBody;
			if (debugEnabled) {
				Logger.Write (LogLevel.Debug, "lt={0} LT::ProcessRecord0 header={1} reqId={2}", listenerTag,
					record.Type, (ushort)((header [2] << 8) + header [3]));
			}
			ulong hash = ((ulong)record.RequestId << 32) ^ listenerTag;  

			TransportRequest request = GetRequest (hash);

			if (request == null && record.Type == RecordType.BeginRequest) {
				BeginRequestBody brb = new BeginRequestBody (recordBody);
				TransportRequest req = new TransportRequest (record.RequestId, header, recordBody);
				req.Hash = ((ulong)record.RequestId << 32) ^ listenerTag;
				req.fd = (uint)listenerTag;
				req.KeepAlive = (brb.Flags & BeginRequestFlags.KeepAlive) == BeginRequestFlags.KeepAlive;
				AddRequest (req);

				FastCgiNetworkConnector connector = FastCgiNetworkConnector.GetConnector (req.fd);
				if (connector != null) {
					connector.KeepAlive = req.KeepAlive;
				}
				return stopReceive;
			}

			switch (record.Type) {
			case RecordType.BeginRequest:
				break;
			case RecordType.Params:
				//TODO: find application in the route
				//TODO: save route to the request
				//FIXME: can be two cases: route not found (no HOST param), need to wait next params  
				//or route not found (there is no matching HOST). Send error back in the case

				if (request.Header != null) {
					if (debugEnabled) {
						Logger.Write (LogLevel.Debug, "lt={0} LT::ProcessRecord header={1} reqId={2}", listenerTag,
							request.Header [1], (ushort)((request.Header [2] << 8) + request.Header [3]));
					}
				}

				if (recordBody != null) {
					FcgiUtils.ParseParameters (recordBody, AddHeader, request);
				} else {
					request.Transport.HeadersSent (request.Hash, request.RequestNumber);
				}

				//send last Params request
				if (debugEnabled) {
					Logger.Write (LogLevel.Debug, "lt={0} LT::ProcessRecord header={1} reqId={2}", listenerTag,
						header [1], (ushort)((header [2] << 8) + header [3]));
				}
				break;
			case RecordType.StandardInput:
				//Ready to process
				if (debugEnabled) {
					Logger.Write (LogLevel.Debug, "lt={0} LT::ProcessRecord header={1} reqId={2}", listenerTag,
						header [1], (ushort)((header [2] << 8) + header [3]));
				}
				bool final = record.BodyLength == 0;
				request.Transport.AddBodyPart (request.Hash, request.RequestNumber, recordBody, final);
				if (final) {
					stopReceive = true;
					request.Transport.Process (request.Hash, request.RequestNumber);
				}
				break;
			case RecordType.Data:
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

		private void AddHeader(string name, string value, bool isHeader, object userData)
		{
			TransportRequest req = userData as TransportRequest;

			//if we did not find a route yet
			if (req.Transport == null) {
				//TODO: change this stub
//				if (isHeader && name == "Host") {
					//TODO: check for null after route if yes return false
					req.Transport = Listener.Server.GetRoute (value).AppHostTransport;
					//TODO: check that transport is routed
					req.Transport.CreateRequest (req.Hash, req.RequestNumber);
					//TODO: send all saved headers.
//					req.Transport.AddHeader (req.Hash, req.RequestNumber, name, value);
//				}
			} 
//			else {
				if (isHeader)
					req.Transport.AddHeader (req.Hash, req.RequestNumber, name, value);
				else
					req.Transport.AddServerVariable (req.Hash, req.RequestNumber, name, value);
//			}
		}

		public void SendOutput (ulong hash, int requestNumber, byte[] data, int length)
		{
			if (debugEnabled) {
				Logger.Write (LogLevel.Debug, "lt={0:X} SendOutput reqN={1}", hash, requestNumber);
			}
			TransportRequest req = GetRequest (hash);
			if (req != null && req.RequestNumber == requestNumber) {
				SendStreamData (req.fd, RecordType.StandardOutput, req.RequestId, data, length);
			} 
		}

		public void EndRequest (ulong hash, int requestNumber, int appStatus)
		{
			if (debugEnabled) {
				Logger.Write (LogLevel.Debug, "lt={0:X} EndRequest reqN={1}", hash, requestNumber);
			}
			TransportRequest req = GetRequest (hash);

			if (req != null && req.RequestNumber == requestNumber) {
				RemoveRequest (req.Hash);
				EndRequestBody body = new EndRequestBody (appStatus, ProtocolStatus.RequestComplete);

				byte[] bodyData = body.GetData ();

				SendRecord (req.fd, RecordType.EndRequest, req.RequestId,
					bodyData, 0, bodyData.Length);
			} else {
				Logger.Write (LogLevel.Error, "Wrong EndRequest"); 
			}
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
			byte[] bodyData, int bodyIndex, int bodyLength)
		{
			if (debugEnabled) {
				Logger.Write (LogLevel.Debug, "at={0} SendRecord header={1} reqId={2}", listenerTag, type, requestID);
			}

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

			SendRecord (listenerTag, header, body);
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
			if (debugEnabled) {
				Logger.Write (LogLevel.Debug, "lt={0} SendRecord", listenerTag);
			}
			FastCgiNetworkConnector connector = FastCgiNetworkConnector.GetConnector (listenerTag);
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

		#region requests caching and handling
		private TransportRequest GetRequest (ulong hash)
		{
			TransportRequest request;

			lock (requestsLock) {
				requests.TryGetValue (hash, out request);
			}

			return request;
		}

		private void AddRequest (TransportRequest request)
		{
			lock (requestsLock) {
				requests.Add (request.Hash, request);
			}
		}

		private void RemoveRequest (ulong hash)
		{
			lock (requestsLock) {
				requests.Remove (hash);
			}
		}
		#endregion

	}
}

