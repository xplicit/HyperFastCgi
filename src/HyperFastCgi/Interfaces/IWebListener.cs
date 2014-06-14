using System;
using System.Net.Sockets;

namespace HyperFastCgi.Interfaces
{
	public interface IWebListener
	{
		IApplicationServer Server { get; }

		IListenerTransport Transport { get; }

		Type AppHostTransportType { get; }

		void Configure (object listenerConfig, IApplicationServer server,
			Type listenerTransport, object listenerTransportConfig,
			Type appHostTransport, object appHostTransportConfig
		);

		/// <summary>
		/// Starts listening incoming requests
		/// </summary>
		/// <returns>0 if success, error code otherwise <returns>
		/// <remarks>
		/// This method uses config options Family, Address and Port
		/// to start listening. Config options are passed by listenerConfig
		/// argument in the Configure method. 
		/// 
		/// Implementation of this method must not block the calling thread
		/// </remarks>
		int Listen ();

		/// <summary>
		/// Shutdowns listening.
		/// </summary>
		void Shutdown ();
	}
}

