using System;
using System.Collections.Generic;
using System.Web;
using System.Globalization;

namespace Mono.WebServer.HyperFastCgi.Requests
{
	public class NativeRequest
	{
		private long requestId;
		private int requestNumber;

		IDictionary<string, string> parameter_table = new Dictionary<string,string> ();

		//cgi request params
		string path;
		string rpath;
		private int port = -1;
		private string vhost = null;
		//headers
		private string[] knownHeaders;
		private Dictionary<string,string> unknownHeadersDict = new Dictionary<string, string> ();
		private string[][] unknownHeaders;

		//post data
		private byte[] input_data;
		private int input_data_offset;

		public byte[] InputData {
			get { return input_data;}
		}

		public long RequestId {
			get { return requestId; }
		}

		public int RequestNumber {
			get { return requestNumber; }
		}

		public NativeRequest (long requestId, int requestNumber)
		{
			this.requestId = requestId;
			this.requestNumber = requestNumber;
			knownHeaders = new string[HttpWorkerRequest.RequestHeaderMaximum];
		}

		public void AddServerVariable(string name, string value)
		{
			parameter_table.Add (name, value);
		}

		public void AddHeader(string header, string value)
		{
			if (!String.IsNullOrEmpty (header)) {
				int idx = HttpWorkerRequest.GetKnownRequestHeaderIndex (header);

				if (idx != -1) {
					knownHeaders [idx] = value;
				} else {
					unknownHeadersDict.Add (header, value);
				}
			}
		}

		public void AddInputData(byte[] data)
		{
			if (input_data == null) {
				int len = 0;
				string slen = GetParameter ("CONTENT_LENGTH");

				if (slen == null) {
					//TODO: error, throw an exception
				}
				if (!int.TryParse (slen, NumberStyles.None, CultureInfo.InvariantCulture, out len)) {
					//TODO: error, throw an exception
				}

				input_data = new byte[len];
			}

			if (input_data_offset + data.Length > input_data.Length) {
				//TODO: throw an exception
			}

			Buffer.BlockCopy (data, 0, input_data, input_data_offset, data.Length);
			input_data_offset += data.Length;
		}


		#region properties, which should be optimized

		public string GetParameter (string parameter)
		{
			if (parameter_table != null && parameter_table.ContainsKey (parameter))
				return (string)parameter_table [parameter];

			return null;
		}

		public IDictionary<string,string> GetParameters ()
		{
			return parameter_table;
		}

		public string Path {
			get {
				if (path == null)
					path = GetParameter ("SCRIPT_NAME");

				return path;
			}
		}

		public string PhysicalPath {
			get {
				if (rpath == null)
					rpath = GetParameter ("SCRIPT_FILENAME");

				return rpath;
			}
		}

		public int PortNumber {
			get {
				if (port < 0)
					port = int.Parse (GetParameter (
						"SERVER_PORT"));

				return port;
			}
		}

		public string HostName {
			get {
				if (vhost == null)
					vhost = GetParameter ("HTTP_HOST");

				return vhost;
			}
		}

		#endregion

		#region ASP.NET support
		public string GetKnownRequestHeader (int index)
		{
			return knownHeaders [index];
		}

		public string GetUnknownRequestHeader (string name)
		{
			if (!unknownHeadersDict.ContainsKey(name))
				return null;

			return unknownHeadersDict [name];
		}

		public string[][] GetUnknownRequestHeaders ()
		{
			if (unknownHeaders == null) {
				unknownHeaders = new string[unknownHeadersDict.Count][];
				Dictionary<string,string>.Enumerator en = unknownHeadersDict.GetEnumerator ();

				for (int i = 0; i < unknownHeadersDict.Count; i++) {
					en.MoveNext ();
					unknownHeaders [i] = new string[] {
						en.Current.Key,
						en.Current.Value
					};
				}
			}
			return unknownHeaders;
		}

		#endregion

	}
}

