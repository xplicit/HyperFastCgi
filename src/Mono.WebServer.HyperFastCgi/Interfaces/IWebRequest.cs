using System;
using System.Collections.Generic;
using System.IO;

namespace Mono.WebServer.HyperFastCgi.Interfaces
{
	public interface IWebRequest
	{
		IDictionary<string,string> RequestHeaders { get; set; }

		void Process (IWebResponse response);

	}
}

