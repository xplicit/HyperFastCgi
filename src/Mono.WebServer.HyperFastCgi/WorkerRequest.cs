//
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
using Mono.WebServer;
using System.Text;
using System.Net;
using System.Globalization;
using System.IO;
using Mono.WebServer.HyperFastCgi.FastCgiProtocol;

namespace Mono.WebServer.HyperFastCgi
{
	public class WorkerRequest : MonoWorkerRequest
	{
		//		private static string [] indexFiles = { "index.aspx",
		//			"default.aspx",
		//			"index.html",
		//			"index.htm" };
		static WorkerRequest ()
		{
			//SetDefaultIndexFiles (System.Configuration.ConfigurationManager.AppSettings ["MonoServerDefaultIndexFiles"]);
		}

		private StringBuilder headers = new StringBuilder ();
		private byte[] input_data;
		private string file_path;
		string raw_url = null;
		private bool closed = false;
		string uri_path = null;
//		string path_info;
		Request cgiRequest;
		NetworkConnector connector;

		public WorkerRequest (NetworkConnector connector, Request cgiRequest, ApplicationHost appHost) : base (appHost)
		{
			this.cgiRequest = cgiRequest;
			this.connector = connector;
			input_data = cgiRequest.InputData;
//			try {
//				//TODO: cache paths
//				Paths.GetPathsFromUri (appHost, GetHttpVerbName (), GetFilePath (), out file_path, out path_info);
//			} catch {
//				path_info = null;
//				file_path = null;
//			}
		}


		#region Overrides

		#region Overrides: Transaction Oriented

		public override int RequestId {
			get { return cgiRequest.RequestId; }
		}

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
			connector.CompleteRequest (cgiRequest.RequestId, 0);
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
			FileStream file = null;
			try {
				file = File.OpenRead (filename);
				SendFromStream (file, offset, length);
			} finally {
				if (file != null)
					file.Close ();
			}
		}

		public override void SendResponseFromFile (IntPtr handle, long offset, long length)
		{
			Stream file = null;
			try {
				#pragma warning disable 618
				file = new FileStream (handle, FileAccess.Read);
				#pragma warning restore
				SendFromStream (file, offset, length);
			} finally {
				if (file != null)
					file.Close ();
			}
		}


		public override void SendResponseFromMemory (byte[] data, int length)
		{
			EnsureHeadersSent ();

			//copy data to temp buffer to be sure that data can't change 
			byte[] buffer = new byte[length];
			Buffer.BlockCopy (data, 0, buffer, 0, length);
			connector.SendOutput (cgiRequest.RequestId, buffer, length);
		}

		public override void SendStatus (int statusCode, string statusDescription)
		{
			AppendHeaderLine ("Status: {0} {1}",
				statusCode, statusDescription);
		}

		public override void SendUnknownResponseHeader (string name, string value)
		{
			AppendHeaderLine ("{0}: {1}", name, value);
		}

		public override bool IsClientConnected ()
		{
			return connector.IsConnected;
		}

		public override bool IsEntireEntityBodyIsPreloaded ()
		{
			return true;
		}

		#endregion

		#region Overrides: Request Oriented

		public override string GetPathInfo ()
		{
			return String.Empty;
//			string pi = cgiRequest.GetParameter ("PATH_INFO");
//			if (!String.IsNullOrEmpty (pi))
//				return pi;
//
//			return path_info ?? String.Empty;
		}

		public override string GetRawUrl ()
		{
			if (raw_url != null)
				return raw_url;

			string fcgiRequestUri = cgiRequest.GetParameter ("REQUEST_URI");
			if (fcgiRequestUri != null) {
				raw_url = fcgiRequestUri;
				return raw_url;
			}

			StringBuilder b = new StringBuilder (GetUriPath ());
			string query = GetQueryString ();
			if (query != null && query.Length > 0) {
				b.Append ('?');
				b.Append (query);
			}

			raw_url = b.ToString ();
			return raw_url;
		}

		public override bool IsSecure ()
		{
			return cgiRequest.GetParameter ("HTTPS") == "on";
		}

		public override string GetHttpVerbName ()
		{
			return cgiRequest.GetParameter ("REQUEST_METHOD");
		}

		public override string GetHttpVersion ()
		{
			return cgiRequest.GetParameter ("SERVER_PROTOCOL");
		}

		public override string GetLocalAddress ()
		{
			string address = cgiRequest.GetParameter ("SERVER_ADDR");
			if (address != null && address.Length > 0)
				return address;

			address = AddressFromHostName (
				cgiRequest.GetParameter ("HTTP_HOST"));
			if (address != null && address.Length > 0)
				return address;

			address = AddressFromHostName (
				cgiRequest.GetParameter ("SERVER_NAME"));
			if (address != null && address.Length > 0)
				return address;

			return base.GetLocalAddress ();
		}

		public override int GetLocalPort ()
		{
			try {
				return cgiRequest.PortNumber;
			} catch {
				return base.GetLocalPort ();
			}
		}

		public override string GetQueryString ()
		{
			return cgiRequest.GetParameter ("QUERY_STRING");
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
			string addr = cgiRequest.GetParameter ("REMOTE_ADDR");
			return addr != null && addr.Length > 0 ?
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
			string port = cgiRequest.GetParameter ("REMOTE_PORT");
			if (port == null || port.Length == 0)
				return base.GetRemotePort ();

			try {
				return int.Parse (port);
			} catch {
				return base.GetRemotePort ();
			}
		}

		public override string GetServerVariable (string name)
		{
			string value = cgiRequest.GetParameter (name);

			if (value == null)
				value = Environment.GetEnvironmentVariable (name);

			return value != null ? value : base.GetServerVariable (name);
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

			file_path = cgiRequest.Path;

			// The following will check if the request was made to a
			// directory, and if so, if attempts to find the correct
			// index file from the list. Case is ignored to improve
			// Windows compatability.

//			string path = cgiRequest.PhysicalPath;
//
//			DirectoryInfo dir = new DirectoryInfo (path);
//
//			if (!dir.Exists)
//				return file_path;
//
//			if (!file_path.EndsWith ("/"))
//				file_path += "/";
//
//			FileInfo [] files = dir.GetFiles ();
//
//			foreach (string file in indexFiles) {
//				foreach (FileInfo info in files) {
//					if (file.Equals (info.Name, StringComparison.InvariantCultureIgnoreCase)) {
//						file_path += info.Name;
//						return file_path;
//					}
//				}
//			}

			return file_path;
		}

		public override string GetUnknownRequestHeader (string name)
		{
			return cgiRequest.GetUnknownRequestHeader (name);
		}

		public override string [][] GetUnknownRequestHeaders ()
		{
			return cgiRequest.GetUnknownRequestHeaders ();
		}

		public override string GetKnownRequestHeader (int index)
		{
			return cgiRequest.GetKnownRequestHeader (index);
		}

		public override string GetServerName ()
		{
			string server_name = HostNameFromString (
				                     cgiRequest.GetParameter ("SERVER_NAME"));

			if (server_name == null)
				server_name = HostNameFromString (
					cgiRequest.GetParameter ("HTTP_HOST"));

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
				connector.SendOutput (cgiRequest.RequestId, str,
					HeaderEncoding);
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

			System.Net.IPAddress[] addresses = null;
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
			if (host == null || host.Length == 0)
				return null;

			int colon_index = host.IndexOf (':');

			if (colon_index == -1)
				return host;

			if (colon_index == 0)
				return null;

			return host.Substring (0, colon_index);
		}
		//		private static void SetDefaultIndexFiles (string list)
		//		{
		//			if (list == null)
		//				return;
		//
		//			List<string> files = new List<string> ();
		//
		//			string [] fs = list.Split (',');
		//			foreach (string f in fs) {
		//				string trimmed = f.Trim ();
		//				if (trimmed == "")
		//					continue;
		//
		//				files.Add (trimmed);
		//			}
		//
		//			indexFiles = files.ToArray ();
		//		}

		#endregion

	}
}
