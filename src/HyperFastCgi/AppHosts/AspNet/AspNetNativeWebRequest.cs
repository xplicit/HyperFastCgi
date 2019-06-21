﻿//
// WorkerRequest.cs: Extends MonoWorkerRequest by getting information from and
// writing information to a NetworkConnector object.
//
// Author:
//
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
using System.Net;
using System.Globalization;
using System.IO;
using HyperFastCgi.Helpers.FastCgiProtocol;
using HyperFastCgi.Interfaces;
using HyperFastCgi.Transports;
using System.Web;
using HyperFastCgi.Helpers.Logging;

namespace HyperFastCgi.AppHosts.AspNet
{
	public class AspNetNativeWebRequest : MonoWorkerRequest, IWebRequest, IWebResponse
	{
		private static string [] indexFiles = { "index.aspx",
			"default.aspx",
			"index.html",
			"index.htm" };

		static AspNetNativeWebRequest ()
		{
			SetDefaultIndexFiles (System.Configuration.ConfigurationManager.AppSettings ["MonoServerDefaultIndexFiles"]);
		}

		private StringBuilder headers = new StringBuilder ();
		private string file_path;
		string raw_url = null;
		private bool closed = false;
		string uri_path = null;
		private bool addTrailingSlash;
		//		string path_info;
		IApplicationHostTransport transport;

		//CGI requests
		private ulong requestId;
		private int requestNumber;

		IDictionary<string, string> parameter_table = new Dictionary<string,string> ();

		//cgi request params
		string path;
		private int port = -1;
		//headers
		private string[] knownHeaders;
		private Dictionary<string,string> unknownHeadersDict = new Dictionary<string, string> ();
		private string[][] unknownHeaders;

		//post data
		private byte[] input_data;
		private int input_data_offset;

		public int PortNumber {
			get
			{
			    if (port < 0)
			    {
			        if (!int.TryParse(GetParameter("SERVER_PORT"), out port)) {
                        Logger.Write(LogLevel.Error, "fastcgi_param 'SERVER_PORT' not set! Setting to default value '80'! "
                            + "Please add 'fastcgi_param SERVER_PORT $server_port;' to your webserver config!");
			            port = 80;
			        }

			    }

                return port;
            }
		}

		public string Path {
			get {
				if (path == null)
					path = GetParameter ("SCRIPT_NAME");

				return path;
			}
		}

		public int RequestNumber {
			get {
				return requestNumber;
			}
		}

		public override ulong RequestId {
			get { return requestId; }
		}

		public IWebRequest Request {
			get { return this;}
		}

		public AspNetNativeWebRequest (ulong requestId, int requestNumber, IApplicationHost appHost, IApplicationHostTransport transport,
			bool addTrailingSlash) : base (appHost)
		{
			this.requestId = requestId;
			this.requestNumber = requestNumber;
			knownHeaders = new string[HttpWorkerRequest.RequestHeaderMaximum];

			this.transport = transport;
			this.addTrailingSlash = addTrailingSlash;
			//			try {
			//				//TODO: cache paths
			//				Paths.GetPathsFromUri (appHost, GetHttpVerbName (), GetFilePath (), out file_path, out path_info);
			//			} catch {
			//				path_info = null;
			//				file_path = null;
			//			}
		}

		public new void AddServerVariable(string name, string value)
		{
			//FIXME: maybe we should use base.AddServerVariable
			parameter_table.Add (name, value);
		}

		public void AddHeader (string header, string value)
		{
			if (!string.IsNullOrEmpty (header)) {
				int idx = HttpWorkerRequest.GetKnownRequestHeaderIndex (header);

				if (idx != -1) {
					knownHeaders [idx] = value;
				} else {
					unknownHeadersDict.Add (header, value);
				}
			}
		}

        /// <summary>
        /// TODO errors can happen here, changing return value to bool and then evaluating the return value outside could help
        /// </summary>
        /// <param name="data"></param>
		public void AddBodyPart (byte[] data)
		{
			if (input_data == null) {
				int len;
				string slen = GetParameter ("CONTENT_LENGTH");

				if (slen == null) {
                    //Instead of throwing an Exception, write to error log and return
                    Logger.Write(LogLevel.Error, "fastcgi_param 'CONTENT_LENGTH' not set! "
                        + "Please add 'fastcgi_param SERVER_PORT $server_port;' to your webserver config!");

                    return;
				}

				if (!int.TryParse (slen, NumberStyles.None, CultureInfo.InvariantCulture, out len)) {
                    Logger.Write(LogLevel.Error, "Invalid CONTENT_LENGTH: {0}", slen);

                    return;
				}

				input_data = new byte[len];
			}

			if (input_data_offset + data.Length > input_data.Length) {
                //Instead of throwing an Exception, write to error log and return
                Logger.Write(LogLevel.Error, "Buffer too small: input_data (size {0}) but should be at least {1}", input_data.Length, (input_data_offset + data.Length));

                return;
			}

            Buffer.BlockCopy (data, 0, input_data, input_data_offset, data.Length);
			input_data_offset += data.Length;
		}

		public string GetParameter (string parameter)
		{
			if (parameter_table != null && parameter_table.ContainsKey (parameter))
				return parameter_table [parameter];

			return null;
		}



		#region Overrides

		#region Overrides: Transaction Oriented


		protected override bool GetRequestData ()
		{
			return true;
		}

		public override bool HeadersSent ()
		{
			return headers == null;
		}

		public override void FlushResponse (bool finalFlush)
		{
			if (finalFlush)
				CloseConnection ();
		}

		public override void CloseConnection ()
		{
			if (closed)
				return;

			closed = true;
			this.EnsureHeadersSent ();
			transport.EndRequest(requestId,requestNumber, 0);
		}

		protected void SendFromStream (Stream stream, long offset, long length)
		{
			if (offset < 0 || length <= 0)
				return;

			long stLength = stream.Length;
			if (offset + length > stLength)
				length = stLength - offset;

			if (offset > 0)
				stream.Seek (offset, SeekOrigin.Begin);

			//TODO: change to Math.Min(length,32760);
			byte[] fileContent = new byte [System.Math.Min(length,Record.SuggestedBodySize)];
			int count = fileContent.Length;
			while (length > 0 && (count = stream.Read (fileContent, 0, count)) != 0) {
				SendResponseFromMemory (fileContent, count);
				length -= count;
				count = (int)System.Math.Min (length, fileContent.Length);
			}
		}

		public override void SendResponseFromFile (string filename, long offset, long length)
		{
			using (FileStream file = File.OpenRead (filename)) {
				SendFromStream (file, offset, length);
			} 
		}

		public override void SendResponseFromFile (IntPtr handle, long offset, long length)
		{
			#pragma warning disable 618
			using (FileStream file = new FileStream (handle, FileAccess.Read)) 
				#pragma warning restore
			{
				SendFromStream (file, offset, length);
			} 
		}


		public override void SendResponseFromMemory (byte[] data, int length)
		{
			EnsureHeadersSent ();

			transport.SendOutput (requestId, requestNumber, data, length);
		}

		public override void SendStatus (int statusCode, string statusDescription)
		{
			AppendHeaderLine ("Status: {0} {1}", statusCode, statusDescription);
		}

		public override void SendUnknownResponseHeader (string name, string value)
		{
			AppendHeaderLine ("{0}: {1}", name, value);
		}

		public override bool IsClientConnected ()
		{
			//FIXME: can we get IsClientConnected?
			return true;
		}

		public override bool IsEntireEntityBodyIsPreloaded ()
		{
			return true;
		}

		#endregion

		#region Overrides: Request Oriented

		public override string GetPathInfo ()
		{
			return GetParameter ("PATH_INFO") ?? string.Empty;
		}

		public override string GetRawUrl ()
		{
			if (raw_url != null)
				return raw_url;

			string fcgiRequestUri = GetParameter ("REQUEST_URI");
			if (fcgiRequestUri != null) {
				raw_url = fcgiRequestUri;
				return raw_url;
			}

			StringBuilder b = new StringBuilder (GetUriPath ());
			string query = GetQueryString ();
			if (!string.IsNullOrEmpty(query)) {
				b.Append ('?');
				b.Append (query);
			}

			raw_url = b.ToString ();
			return raw_url;
		}

		public override bool IsSecure ()
		{
			return GetParameter ("HTTPS") == "on";
		}

		public override string GetHttpVerbName ()
		{
			return GetParameter ("REQUEST_METHOD");
		}

		public override string GetHttpVersion ()
		{
			return GetParameter ("SERVER_PROTOCOL");
		}

		public override string GetLocalAddress ()
		{
			string address = GetParameter ("SERVER_ADDR");
			if (!string.IsNullOrEmpty(address))
				return address;

			address = AddressFromHostName (
				GetParameter ("HTTP_HOST"));
			if (!string.IsNullOrEmpty(address))
				return address;

			address = AddressFromHostName (
				GetParameter ("SERVER_NAME"));
			if (!string.IsNullOrEmpty(address))
				return address;

			return base.GetLocalAddress ();
		}

		public override int GetLocalPort ()
		{
			try {
				return PortNumber;
			} catch {
				return base.GetLocalPort ();
			}
		}

		public override string GetQueryString ()
		{
			return GetParameter ("QUERY_STRING");
		}

		public override byte [] GetQueryStringRawBytes ()
		{
			string query_string = GetQueryString ();
			if (query_string == null)
				return null;
			return Encoding.GetBytes (query_string);
		}

		public override string GetRemoteAddress ()
		{
			string addr = GetParameter ("REMOTE_ADDR");
			return !string.IsNullOrEmpty(addr) ?
				addr : base.GetRemoteAddress ();
		}

		public override string GetRemoteName ()
		{
			string ip = GetRemoteAddress ();
			string name = null;
			try {
				IPHostEntry entry = Dns.GetHostEntry (ip);
				name = entry.HostName;
			} catch {
				name = ip;
			}

			return name;
		}

		public override int GetRemotePort ()
		{
			string port = GetParameter ("REMOTE_PORT");
			if (string.IsNullOrEmpty(port))
				return base.GetRemotePort ();

			try {
				return int.Parse (port);
			} catch {
				return base.GetRemotePort ();
			}
		}

		public override string GetServerVariable (string name)
		{
			string value = GetParameter (name);

			if (value == null && name != null)
				value = Environment.GetEnvironmentVariable (name);

			return value ?? base.GetServerVariable (name);
		}

		public override string GetUriPath ()
		{
			if (uri_path != null)
				return uri_path;

			uri_path = GetFilePath () + GetPathInfo ();
			return uri_path;
		}

		public override string GetFilePath ()
		{
			if (file_path != null)
				return file_path;

			file_path = Path;

			// The following will check if the request was made to a
			// directory, and if so, if attempts to find the correct
			// index file from the list. Filename case is ignored to improve
			// Windows compatability.
			if (addTrailingSlash) {
				string path = MapPath (file_path); //or just path = cgiRequest.PhysicalPath

				DirectoryInfo dir = new DirectoryInfo (path);

				if (!dir.Exists)
					return file_path;

				if (!file_path.EndsWith ("/", StringComparison.OrdinalIgnoreCase))
					file_path += "/";

				FileInfo[] files = dir.GetFiles ();

				foreach (string file in indexFiles) {
					foreach (FileInfo info in files) {
						if (file.Equals (info.Name, StringComparison.OrdinalIgnoreCase)) {
							file_path += info.Name;
							return file_path;
						}
					}
				}
			}

			return file_path;
		}

		public override string GetUnknownRequestHeader (string name)
		{
			if (!unknownHeadersDict.ContainsKey(name))
				return null;

			return unknownHeadersDict [name];
		}

		public override string [][] GetUnknownRequestHeaders ()
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

		public override string GetKnownRequestHeader (int index)
		{
			return knownHeaders [index];
		}

		public override string GetServerName ()
		{
			string server_name = HostNameFromString (
				GetParameter ("SERVER_NAME"));

			if (server_name == null)
				server_name = HostNameFromString (
					GetParameter ("HTTP_HOST"));

			if (server_name == null)
				server_name = GetLocalAddress ();

			return server_name;
		}

		public override byte [] GetPreloadedEntityBody ()
		{
			return input_data;
		}

		#endregion

		#endregion

		#region Private Methods

		private void AppendHeaderLine (string format, params object[] args)
		{
			if (headers == null)
				return;

			headers.AppendFormat (CultureInfo.InvariantCulture,
				format, args);
			headers.Append ("\r\n");
		}

		private void EnsureHeadersSent ()
		{
			if (headers != null) {
				headers.Append ("\r\n");
				string str = headers.ToString ();
				byte[] data = HeaderEncoding.GetBytes (str);
				transport.SendOutput (requestId, requestNumber, data, data.Length);
				headers = null;
			}
		}

		#endregion

		#region Private Static Methods

		private static string AddressFromHostName (string host)
		{
			host = HostNameFromString (host);

			if (host == null || host.Length > 126)
				return null;

			IPAddress[] addresses = null;
			try {
				addresses = Dns.GetHostAddresses (host);
			} catch (System.Net.Sockets.SocketException) {
				return null;
			} catch (ArgumentException) {
				return null;
			}

			if (addresses == null || addresses.Length == 0)
				return null;

			return addresses [0].ToString ();
		}

		private static string HostNameFromString (string host)
		{
			if (string.IsNullOrEmpty(host))
				return null;

			int colon_index = host.IndexOf (':');

			if (colon_index == -1)
				return host;

			if (colon_index == 0)
				return null;

			return host.Substring (0, colon_index);
		}

		private static void SetDefaultIndexFiles (string list)
		{
			if (list == null)
				return;

			List<string> files = new List<string> ();

			string [] fs = list.Split (',');
		    for (int index = 0; index < fs.Length; index++)
		    {
		        string f = fs[index];
		        string trimmed = f.Trim();
		        if (trimmed == "")
		            continue;

		        files.Add(trimmed);
		    }

		    indexFiles = files.ToArray ();
		}

		#endregion

		#region IWebRequest implementation
		public void Process (IWebResponse response)
		{
			base.ProcessRequest ();
		}
		#endregion

		#region IWebResponse implementation
		public void Send (int status, string description, IDictionary<string, string> sendHeaders)
		{
			SendStatus (status, description);
			foreach (KeyValuePair<string, string> pair in sendHeaders) {
				SendUnknownResponseHeader (pair.Key, pair.Value);
			}
		}
		public void Send (int status, string description, IDictionary<string, string> sendHeaders, byte[] response)
		{
			Send (status, description, sendHeaders);
			SendResponseFromMemory (response, response.Length);
		}
		public void Send (byte[] response)
		{
			SendResponseFromMemory (response, response.Length);
		}
		public void Send (Stream stream, long offset, long length)
		{
			SendFromStream (stream, offset, length);
		}
		public void CompleteResponse ()
		{
			throw new NotImplementedException ();
		}
		public IDictionary<string, string> RequestHeaders {
			get {
				throw new NotImplementedException ();
			}
			set {
				throw new NotImplementedException ();
			}
		}
		#endregion
	}
}
