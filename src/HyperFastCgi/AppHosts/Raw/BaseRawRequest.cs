using System;
using HyperFastCgi.Interfaces;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace HyperFastCgi.AppHosts.Raw
{
	public class BaseRawRequest : IWebRequest, IWebResponse
	{
		Dictionary<string,string> headers=new Dictionary<string, string>();
		Dictionary<string,string> serverVars=new Dictionary<string, string>();
		Dictionary<string,string> responseHeaders=new Dictionary<string, string>();
		private StringBuilder responseHeadersOutput = new StringBuilder ();
		bool responseHeadersSent;

		byte[] inputData;
		int offset;

		ulong requestId;
		int requestNumber;

		IApplicationHost appHost;
		IApplicationHostTransport transport;

		public virtual byte[] GetPreloadedEntityBody()
		{
			return inputData;
		}

		public IApplicationHost AppHost {
			get { return appHost; }
		}

		public virtual void Configure(ulong requestId, int requestNumber, IApplicationHost appHost)
		{
			this.requestId = requestId;
			this.requestNumber = requestNumber;
			this.appHost = appHost;
			this.transport = appHost.AppHostTransport;
		}
		#region WebResponse extensions
		public int Status { get; set; }

		public string StatusDescription { get; set; }

		public long? ContentLength { get; set; }

		public IDictionary<string, string> ResponseHeaders { get { return responseHeaders; } }

		#endregion

		#region IWebRequest implementation

		public virtual void AddServerVariable (string name, string value)
		{
			serverVars.Add (name, value);
		}

		public virtual void AddHeader (string name, string value)
		{
			headers.Add (name, value);
		}

		public virtual void AddBodyPart (byte[] data)
		{
			if (inputData == null) {
				string slen;

				if (serverVars.TryGetValue ("CONTENT_LENGTH", out slen)) {
					int len;

					if (int.TryParse (slen, out len)) {
						inputData = new byte[len];
					}
				}
			}
			Buffer.BlockCopy (data, 0, inputData, offset, data.Length);
			offset += data.Length;
		}

		public virtual void Process (IWebResponse response)
		{
			throw new NotImplementedException ();
		}

		public virtual ulong RequestId {
			get {
				return requestId;
			}
		}

		public virtual int RequestNumber {
			get {
				return requestNumber;
			}
		}

		public IDictionary<string, string> RequestHeaders {
			get {
				return headers;
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public IDictionary<string, string> ServerVariables {
			get {
				return serverVars;
			}
		}
		#endregion

		#region IWebResponse implementation

		public void Send (int status, string description, IDictionary<string, string> headers)
		{
			Status = status;
			StatusDescription = description;
			responseHeaders = (Dictionary<string,string>)headers;
		}

		public void Send (int status, string description, IDictionary<string, string> headers, byte[] response)
		{
			Send (status, description, headers);
			Send (response);
		}

		public void Send (byte[] response)
		{
			if (!responseHeadersSent) {
				byte[] prepared = PrepareHeaders (response.Length);
				transport.SendOutput (requestId, requestNumber, prepared, prepared.Length);
				responseHeadersSent = true;
			}
			transport.SendOutput (requestId, requestNumber, response, response.Length); 
		}

		public void Send (System.IO.Stream stream, long offset, long length)
		{
			throw new NotImplementedException ();
		}

		public virtual void CompleteResponse ()
		{
			transport.EndRequest (requestId, requestNumber, 0);
		}

		public IWebRequest Request {
			get {
				return this;
			}
		}

		#endregion

		private byte[] PrepareHeaders(int contentLength)
		{
			responseHeadersOutput.AppendFormat (CultureInfo.InvariantCulture,
				"Status: {0} {1}\r\n", Status, StatusDescription); 
			if (!ContentLength.HasValue || ContentLength.Value < contentLength) {
				ContentLength = contentLength;
			}
			responseHeadersOutput.AppendFormat (CultureInfo.InvariantCulture,
				"Content-Length: {0}\r\n", ContentLength);

			foreach (KeyValuePair<string,string> pair in responseHeaders) {
				if (!pair.Key.Equals("Content-Length",StringComparison.OrdinalIgnoreCase) &&
					!pair.Key.Equals("Status",StringComparison.OrdinalIgnoreCase)
				) {
					responseHeadersOutput.AppendFormat (CultureInfo.InvariantCulture,
						"{0}: {1}\r\n", pair.Key, pair.Value);
				}
			}
			responseHeadersOutput.Append ("\r\n");

			return Encoding.GetEncoding (28591)
				.GetBytes (responseHeadersOutput.ToString ());
		}
	}
}

