using System;
using HyperFastCgi.Interfaces.Events;

namespace HyperFastCgi.Interfaces
{
	public interface IApplicationHost
	{
		string Path { get; }

		string VPath { get; }

		string VHost { get; }

		int VPort { get; }

		AppDomain Domain { get; }

		IApplicationHostTransport AppHostTransport { get; }

		IListenerTransport ListenerTransport { get; } 

		IApplicationServer Server { get; }

		event EventHandler<HostUnloadEventArgs> HostUnload;

		/// <summary>
		/// Init the host with transport.
		/// </summary>
		/// <param name="appServer">Application Server</param>
		/// <param name="transportType">Transport type.</param>
		/// <param name="transportConfig">Transport config.</param>
		/// <param name="appHostConfig">Application host config.</param>
		/// <remarks>This method is called by Application Server from other AppDomain.
		/// </remarks>
		void Configure (object appHostConfig, object webAppConfig,
			IApplicationServer appServer, 
			IListenerTransport listenerTransport, 
			Type appHostTransportType, object appHostTransportConfig);

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

		/// <summary>
		/// Shutdown the application.
		/// </summary>
		void Shutdown();

	}
}

