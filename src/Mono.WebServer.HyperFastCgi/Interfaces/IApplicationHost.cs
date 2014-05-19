using System;

namespace Mono.WebServer.HyperFastCgi.Interfaces
{
	public interface IApplicationHost
	{
		string Path { get; }

		string VPath { get; }

		/// <summary>
		/// Init the host with transport.
		/// </summary>
		/// <param name="appServer">Application Server</param>
		/// <param name="transportType">Transport type.</param>
		/// <param name="transportConfig">Transport config.</param>
		/// <remarks>This method is called by Application Server from other AppDomain.
		/// </remarks>
		void Init (IApplicationServer appServer, Type transportType, object transportConfig);

		/// <summary>
		/// Creates the request.
		/// </summary>
		/// <returns>The request.</returns>
		/// <param name="requestId">Unique request identifier is passed by transport.</param>
		/// <param name="requestNumber">Request number is passed by transport.</param>
		/// <param name="arg">User-defined argument, which can be used to pass additional data by 
		/// custom transport</param>
		/// <remarks>This method is called by transport when new request comes from front-end</remarks>
		IWebRequest CreateRequest (ulong requestId, int requestNumber, object arg);

		/// <summary>
		/// Gets the web response.
		/// </summary>
		/// <returns>The response.</returns>
		/// <param name="request">Request</param>
		/// <param name="arg">User-defined argument</param>
		/// <remarks>This method is called by transport to get WebResponse and then process request.
		/// If request implements both IWebRequest and IWebResponse interfaces it just returns 'this'</remarks>
		IWebResponse GetResponse (IWebRequest request, object arg);

		void ProcessRequest (IWebRequest request);

		IListenerTransport GetAppHostTransport();

		IListenerTransport GetListenerTransport (); 

		IApplicationServer Server { get; }
	}
}

