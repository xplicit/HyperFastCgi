using System;
using HyperFastCgi.Interfaces;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text;
using HyperFastCgi.Helpers.Logging;

namespace HyperFastCgi.Transports
{
	public class CombinedAppHostTransport : BaseAppHostTransport
	{

		#region INativeTransport implementation
		protected override void OnHostUnload (IApplicationHost host, bool isShutdown)
		{
			//remove apphost transport from list of hosts in unmanaged code
			//when the domain unloads
			UnregisterHost (
				host.VHost,
				host.VPort,
				host.VPath
			);		
		}

		public override void Configure (IApplicationHost host, object config)
		{
			base.Configure (host, config);

			try {
				//add apphost transport to list of hosts in unmanaged code
				RegisterHost (host.VHost, host.VPort, host.VPath, host.Path);

				//We have to call RegisterAppHostTransport each time
				//otherwise when domain unloaded it frees jit-tables
				//for classes were loaded in this domain. This means
				//that calling unmanaged thunks pointed to old jitted methods
				//in native-to-managed wrapper produces SEGFAULT. 
				//Reregistering thunks via RegisterAppHostTransport helps to avoid the issue.
				RegisterAppHostTransport(this.GetType());
			} catch (Exception ex){
				Logger.Write (LogLevel.Error, "Error in configuring CombinedAppHostTransport {0}", ex);
			}
		}

		#endregion

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void RegisterAppHostTransport (Type transportType);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void RegisterHost (string vhost, int vport, string virtualPath, string path);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void UnregisterHost (string vhost, int vport, string virtualPath);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern override void SendOutput (ulong requestId, int requestNumber, byte[] data, int len);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern override void EndRequest (ulong requestId, int requestNumber, int appStatus);

	}
}



