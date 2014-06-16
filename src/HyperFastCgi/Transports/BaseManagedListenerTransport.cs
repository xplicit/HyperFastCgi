using System;
using HyperFastCgi.Interfaces;
using HyperFastCgi.Listeners;
using HyperFastCgi.Helpers.FastCgiProtocol;
using HyperFastCgi.Helpers.Logging;
using System.Collections.Generic;
using Mono.WebServer;

namespace HyperFastCgi.Transports
{
	/// <summary>
	/// Base class for transports which deals with managed listener.
	/// </summary>
	public abstract class BaseManagedListenerTransport : MarshalByRefObject, IListenerTransport
	{
		#region abstract methods
		public abstract void CreateRequest(TransportRequest req);

		public abstract void AddHeader(TransportRequest req, string name, string value);

		public abstract void AddServerVariable(TransportRequest req, string name, string value);

		public abstract void HeadersSent(TransportRequest req);

		public abstract void AddBodyPart(TransportRequest req, byte[] body, bool final);

		public abstract void Process(TransportRequest req);

		public abstract bool IsHostFound(TransportRequest req);

		public abstract void GetRoute(TransportRequest req, string vhost, int vport, string vpath);
		#endregion

		private Dictionary<ulong,TransportRequest> requests = new Dictionary<ulong, TransportRequest> ();
		private static object requestsLock = new object ();
		bool debugEnabled = false;
		private IWebListener listener;

		public IWebListener Listener {
			get { return listener;}
		} 

		#region IListenerTransport implementation
		public virtual void Configure (IWebListener listener, object config)
		{
			this.listener = listener;
		}

		public virtual bool Process (ulong listenerTag, int requestNumber, byte[] header, byte[] recordBody)
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
				TransportRequest req = new TransportRequest (record.RequestId);
				req.Hash = ((ulong)record.RequestId << 32) ^ listenerTag;
				req.fd = (uint)listenerTag;
				req.KeepAlive = (brb.Flags & BeginRequestFlags.KeepAlive) == BeginRequestFlags.KeepAlive;
				AddRequest (req);

				//try to find single app route
				GetRoute (req, null, -1, null);
				if (IsHostFound(req)) {
					CreateRequest (req);
				}

				FastCgiNetworkConnector connector = FastCgiNetworkConnector.GetConnector (req.fd);
				if (connector != null) {
					connector.KeepAlive = req.KeepAlive;
				}
				return stopReceive;
			}

			if (request != null) {
				switch (record.Type) {
				case RecordType.BeginRequest:
					break;
				case RecordType.Params:
					if (header != null) {
						if (debugEnabled) {
							Logger.Write (LogLevel.Debug, "lt={0} LT::ProcessRecord header={1} reqId={2}", listenerTag,
								header [1], (ushort)((header [2] << 8) + header [3]));
						}
					}

					if (recordBody != null) {
						FcgiUtils.ParseParameters (recordBody, AddHeader, request);
					} else {
						//FIXME: request.Host can be null
						HeadersSent (request);
					}

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
					AddBodyPart (request, recordBody, final);
					if (final) {
						stopReceive = true;
						Process (request);
					}
					break;
				case RecordType.Data:
					break;
				case RecordType.GetValues:
					//TODO: return server values
					break;
				// Aborts a request when the server aborts.
				//TODO: make Thread.Abort for request
				case RecordType.AbortRequest:
					//FIXME: send it to the HostTransport as is
					//TODO: send error to Connector
					//TODO: send EndRequest to Connector
					//					SendError (request.RequestId, Strings.Connection_AbortRecordReceived);
					//					EndRequest (request.RequestId, -1, ProtocolStatus.RequestComplete);
					break;

				default:
				//TODO: CgiConnector.SendRecord
				//				SendRecord (new Record (Record.ProtocolVersion,
				//					RecordType.UnknownType,
				//					request.RequestId,
				//					new UnknownTypeBody (record.Type).GetData ()));
					break;
				}
			}

			return stopReceive;

		}

		public virtual void SendOutput (ulong hash, int requestNumber, byte[] data, int length)
		{
			if (debugEnabled) {
				Logger.Write (LogLevel.Debug, "lt={0:X} SendOutput reqN={1}", hash, requestNumber);
			}
			TransportRequest req = GetRequest (hash);
			if (req != null && req.RequestNumber == requestNumber) {
				SendStreamData (req.fd, RecordType.StandardOutput, req.RequestId, data, length);
			} 
		}

		public virtual void EndRequest (ulong hash, int requestNumber, int appStatus)
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
		#endregion

		private bool AddHeader(string name, string value, bool isHeader, object userData)
		{
			TransportRequest req = userData as TransportRequest;

			//if we did not find a route yet
			if (!IsHostFound(req)) {
				req.tempKeys.Add (new KeyValuePair () {
					Key = name,
					Value = value,
					IsHeader = isHeader
				});

				if (req.VHost == null && name == "SERVER_NAME") {
					req.VHost = value;
				}
				if (req.VPort == -1 && name == "SERVER_PORT") {
					int.TryParse (value, out req.VPort);
				}
				if (req.VPath == null && name == "SCRIPT_NAME") {
					req.VPath = value;
				}

				if (req.VHost != null && req.VPort != -1 && req.VPath != null) {
					GetRoute (req, req.VHost, req.VPort, req.VPath);

					if (IsHostFound(req)) {
						CreateRequest (req);

						foreach (KeyValuePair pair in req.tempKeys) {
							if (pair.IsHeader)
								AddHeader (req, pair.Key, pair.Value);
							else
								AddServerVariable (req, pair.Key, pair.Value);
						}
					} else {
						Logger.Write (LogLevel.Error, "Can't find app {0}:{1} {2}", req.VHost, req.VPort, req.VPath);
						//TODO: Send EndRequest with error message
						//SendError (request.Hash, req.RequestNumber, Strings.Connection_AbortRecordReceived);
						byte[] notFound = HttpErrors.NotFound (req.VPath);
						SendOutput (req.Hash, req.RequestNumber, notFound, notFound.Length); 
						EndRequest (req.Hash, req.RequestNumber, 0);
						return false;
					}
				}
			} else {
				if (isHeader)
					AddHeader (req, name, value);
				else
					AddServerVariable (req, name, value);
			}

			return true;
		}

		#region SendData implementation
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

		private void SendRecord (uint listenerTag, RecordType type, ushort requestID,
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
		private bool SendRecord (uint listenerTag, byte[] header,byte[] body)
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
		#endregion

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

