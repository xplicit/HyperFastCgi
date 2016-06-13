using System;
using System.Collections.Generic;
using System.IO;

namespace HyperFastCgi.Interfaces
{
	public interface IWebResponse
	{
		IWebRequest Request { get; }

		void Send (int status, string description, IDictionary<string,string> sendHeaders);

		void Send (int status, string description, IDictionary<string,string> sendHeaders, byte[] response);

		void Send (byte[] response);

		void Send (Stream stream, long offset, long length);

		void CompleteResponse ();
	}
}

