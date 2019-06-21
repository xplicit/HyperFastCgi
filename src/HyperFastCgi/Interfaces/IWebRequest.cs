using System;
using System.Collections.Generic;
using System.IO;

namespace HyperFastCgi.Interfaces
{
	public interface IWebRequest
	{
		/// <summary>
		/// Unique request number 
		/// </summary>
		/// <value>The request identifier.</value>
		/// <remarks>RequestId is unique number between simultaniously served requests. 
		/// RequestId is used by Listener to know which front-end request match to 
		/// IWebRequest
		/// </remarks>
		ulong RequestId { get; }

		/// <summary>
		/// Gets or sets the request number.
		/// </summary>
		/// <value>The request number.</value>
		/// <remarks>Request number performs CRC role for the request. If front-end aborts 
		/// request and creates new one with the same RequestId, while IWebRequest
		/// being processed by application server, Listener will know, that the  
		/// old IWebRequest are not the same, which is created by front-end, and won't
		/// sent data belonging to old IWebRequest to new front-end request.  
		/// </remarks>
		int RequestNumber { get; }

		/// <summary>
		/// Adds the server variable.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		/// <remarks>This method is called by transport when new server variable has come</remarks>
		void AddServerVariable (string name, string value);

		/// <summary>
		/// Adds the header.
		/// </summary>
		/// <param name="name">Header name.</param>
		/// <param name="value">Header value.</param>
		/// <remarks>This method is called by transport when new header has come</remarks> 
		void AddHeader(string name, string value);

		/// <summary>
		/// Adds the content data
		/// TODO: Change return value to "bool" to be able to report (buffer) errors?
		/// </summary>
		/// <param name="data">content data</param>
		/// <remarks>The method is called by transport to add the part of content (post) data. 
		/// When all data is read Process() called</remarks>
		void AddBodyPart (byte[] data);

		/// <summary>
		/// Headers collection
		/// </summary>
		/// <value>The request headers.</value>
		IDictionary<string,string> RequestHeaders { get; }

		/// <summary>
		/// Processes the request.
		/// </summary>
		/// <param name="response">Response</param>
		/// <remarks>The method is called by transport to process Web Request</remarks>
		void Process (IWebResponse response);
	}
}

