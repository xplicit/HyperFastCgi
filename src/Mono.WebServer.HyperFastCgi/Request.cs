//
// Record.cs: Handles FastCGI requests
//
// Author:
//   Sergey Zhukov
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
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
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Globalization;
using Mono.WebServer.HyperFastCgi.FastCgiProtocol;
using Mono.WebServer.HyperFastCgi.Logging;

namespace Mono.WebServer.HyperFastCgi
{
	public class Request
	{
		private ushort requestId;
		private List<byte> parameter_data = new List<byte> ();
		IDictionary<string, string> parameter_table = new Dictionary<string,string> ();
		private byte[] input_data;
		int input_data_offset;
		private	bool input_data_completed;
		private static Encoding encoding = Encoding.Default;
		//cgi request params
		string path;
		string rpath;
		private int port = -1;
		private string vhost = null;
		//headers
		private string[] knownHeaders;
		private Dictionary<string,string> unknownHeadersDict = new Dictionary<string, string> ();
		private string[][] unknownHeaders;

		public ushort RequestId {
			get { return requestId; }
		}

		public bool StdOutSent;
		public bool StdErrSent;

		public Request (ushort requestId)
		{
			this.requestId = requestId;
			knownHeaders = new string[HttpWorkerRequest.RequestHeaderMaximum];
		}

		public byte [] InputData {
			get { return input_data != null ? input_data : new byte [0]; }
		}

		public string GetKnownRequestHeader (int index)
		{
			return knownHeaders [index];
		}

		public string[][] GetUnknownRequestHeaders ()
		{
			return unknownHeaders;
		}

		public string GetUnknownRequestHeader (string name)
		{
			if (!unknownHeadersDict.ContainsKey(name))
				return null;

			return unknownHeadersDict [name];
		}

		public bool AddParameterData (byte[] data, bool parseHeaders)
		{
			// Validate arguments in public methods.
			if (data == null)
				throw new ArgumentNullException ("data");

			// When all the parameter data is received, it is acted
			// on and the parameter_data object is nullified.
			// Further data suggests a problem with the HTTP server.
			if (parameter_data == null) {
				Logger.Write (LogLevel.Warning,
					Strings.Request_ParametersAlreadyCompleted);
				return true;
			}

			// If data was provided, append it to that already
			// received, and exit.
			if (data.Length > 0) {
				//parameter_data.AddRange (data);
				ParseParameters (data, parseHeaders);
				return false;
			}

			if (parseHeaders) {
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
			return true;
//			ParseParameterData ();
		}

		private void ParseParameters (byte[] data, bool parseHeaders)
		{
			int dataLength = data.Length;
			int offset = 0;
			int nlen, vlen;
			string name, value;
			//TODO: can encoding change?
			Encoding enc = encoding;

			while (offset < dataLength) {
				nlen = data [offset++];

				if (nlen >= 0x80) {
					nlen = ((0x7F & nlen) * 0x1000000)
					+ ((int)data [offset++]) * 0x10000
					+ ((int)data [offset++]) * 0x100
					+ ((int)data [offset++]);
				}

				vlen = data [offset++];

				if (vlen >= 0x80) {
					vlen = ((0x7F & vlen) * 0x1000000)
					+ ((int)data [offset++]) * 0x10000
					+ ((int)data [offset++]) * 0x100
					+ ((int)data [offset++]);
				}

				// Do a sanity check on the size of the data.
				if (offset + nlen + vlen > dataLength)
					throw new ArgumentOutOfRangeException ("offset");

				name = enc.GetString (data, offset, nlen);
				offset += nlen;
				value = enc.GetString (data, offset, vlen);
				offset += vlen;

				parameter_table.Add (name, value);

				if (parseHeaders) {
					string header = ReformatHttpHeader (name);

					if (!String.IsNullOrEmpty (header)) {
						int idx = HttpWorkerRequest.GetKnownRequestHeaderIndex (header);

						if (idx != -1) {
							knownHeaders [idx] = value;
						} else {
							unknownHeadersDict.Add (header, value);
						}
					}
				}
			}
		}

		private static string ReformatHttpHeader (string name)
		{
			if (name.StartsWith ("HTTP_", StringComparison.Ordinal)) {
				char[] header = new char[name.Length - 5];

				// "HTTP_".Length
				int i = 5;
				bool upperCase = true;

				while (i < name.Length) {
					if (name [i] == '_') {
						header [i - 5] = '-';
						upperCase = true;
					} else {
						header [i - 5] = (upperCase) ? name [i] : char.ToLower (name [i]);
						upperCase = false;
					}
					i++; 
				}

				return new string (header);
			} 

			return String.Empty;
		}

		public bool AddInputData (Record record)
		{
			// Validate arguments in public methods.
			if (record.Type != RecordType.StandardInput)
				throw new ArgumentException (
					Strings.Request_NotStandardInput,
					"record");

			// There should be no data following a zero byte record.
			if (input_data_completed) {
				Logger.Write (LogLevel.Warning,
					Strings.Request_StandardInputAlreadyCompleted);
				return input_data_completed;
			}

			if (record.BodyLength == 0)
				input_data_completed = true;
			else {
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

				if (input_data_offset + record.BodyLength > input_data.Length) {
					//TODO: throw an exception
				}

				Buffer.BlockCopy (record.Body, record.BodyOffset, input_data, input_data_offset, record.BodyLength);
				//Array.Copy (record.Body, record.BodyOffset, input_data, input_data_offset, record.BodyLength);
				input_data_offset += record.BodyLength;
			}

			// Inform listeners of the data.
			/*if (InputDataReceived != null)
				InputDataReceived (this, new DataReceivedArgs (record));*/

			return input_data_completed;
		}

		private bool file_data_completed = false;

		public bool AddFileData (Record record)
		{
			// Validate arguments in public methods.
			if (record.Type != RecordType.Data)
				throw new ArgumentException (
					Strings.Request_NotFileData,
					"record");

			// There should be no data following a zero byte record.
			if (file_data_completed) {
				Logger.Write (LogLevel.Warning,
					Strings.Request_FileDataAlreadyCompleted);
				return false;
			}

			if (record.BodyLength == 0)
				file_data_completed = true;

			// Inform listeners of the data.
//			if (FileDataReceived != null)
//				FileDataReceived (this, new DataReceivedArgs (record));
			return file_data_completed;
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

	}
}

