using System;

namespace Mono.WebServer.HyperFastCgi.Interfaces
{
	public interface IApplicationHost
	{
		string Path { get; }

		string VPath { get; }

		/// <summary>
		/// Creates the request.
		/// </summary>
		/// <returns>The request.</returns>
		/// <param name="requestId">Unique request identifier is passed by transport.</param>
		/// <param name="requestNumber">Request number is passed by transport.</param>
		/// <param name="arg">User-defined argument, which can be used to pass additional data by 
		/// custom transport</param>
		IWebRequest CreateRequest (ulong requestId, int requestNumber, object arg);

		/// <summary>
		/// Gets the web response.
		/// </summary>
		/// <returns>The response.</returns>
		/// <param name="request">Request</param>
		/// <param name="arg">User-defined argument</param>
		/// <remarks>If request implements both IWebRequest and IWebResponse interfaces it just returns 'this'</remarks>
		IWebResponse GetResponse (IWebRequest request, object arg);

		void ProcessRequest (IWebRequest request);

		IListenerTransport GetAppHostTransport();

		IListenerTransport GetListenerTransport (); 

		IApplicationServer Server { get; set;}
	}
}

