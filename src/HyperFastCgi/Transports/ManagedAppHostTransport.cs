using System;
using HyperFastCgi.Interfaces;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text;
#if !NET_2_0
using System.Threading.Tasks;
#endif 

namespace HyperFastCgi.Transports
{
	public class ManagedAppHostTransport : BaseAppHostTransport
	{
		bool isUnload;

		#region BaseAppHostTransport overrides

		protected override void OnHostUnload (IApplicationHost host, bool isShutdown)
		{
			isUnload = true;
		}

		public override void SendOutput (ulong requestId, int requestNumber, byte[] data, int len)
		{
			if (!isUnload) {
				AppHost.ListenerTransport.SendOutput (requestId, requestNumber, data, len);
			}
		}

		public override void EndRequest (ulong requestId, int requestNumber, int appStatus)
		{
			if (!isUnload) {
				AppHost.ListenerTransport.EndRequest (requestId, requestNumber, appStatus);
			}
		}

		#endregion
	}
}


