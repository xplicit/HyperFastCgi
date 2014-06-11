using System;
using HyperFastCgi.Interfaces;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text;
using HyperFastCgi.Logging;


#if !NET_2_0
using System.Threading.Tasks;
#endif 

namespace HyperFastCgi.Transports
{
	public class CombinedAppHostTransport : MarshalByRefObject, IApplicationHostTransport
	{
		Dictionary<ulong, IWebRequest> requests = new Dictionary<ulong, IWebRequest> ();
		object requestsLock = new object ();
		IApplicationHost appHost;

		public IApplicationHost AppHost {
			get { return appHost;}
		}

		#region INativeTransport implementation

		public void Configure (IApplicationHost host, object config)
		{
			try {
				this.appHost = host;
				//remove apphost transport from list of hosts in unmanaged code
				//when the domain unloads
				host.HostUnload += (sender, e) => UnregisterHost (
					((IApplicationHost)sender).VHost,
					((IApplicationHost)sender).VPort,
					((IApplicationHost)sender).VPath
				);
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

		public void CreateRequest (ulong requestId, int requestNumber)
		{
			Console.WriteLine ("reqN={0}", requestNumber);
			IWebRequest req=AppHost.CreateRequest (requestId, requestNumber, null);

			lock (requestsLock) {
				requests.Add (requestId, req);
			}

			//			Console.WriteLine ("CreateRequest hash={0}, reqN={1}", requestId, requestNumber);
		}

		public void AddServerVariable (ulong requestId, int requestNumber, string name, string value)
		{
			IWebRequest request;
			lock (requestsLock)
			{
				requests.TryGetValue (requestId, out request);
			}

			try
			{
				if (request != null 
					&& request.RequestNumber == requestNumber) {
					request.AddServerVariable (name, value);
				}
			} catch (Exception ex) {
				Console.WriteLine ("ex={0}", ex.ToString ());
			}
		}

		public void AddHeader (ulong requestId, int requestNumber, string name, string value)
		{
			IWebRequest request;
			lock (requestsLock)
			{
				requests.TryGetValue (requestId, out request);
			}

			if (request != null
				&& request.RequestNumber == requestNumber) {
				request.AddHeader (name, value);
			}
		}

		public void HeadersSent (ulong requestId, int requestNumber)
		{

		}

		public void AddBodyPart (ulong requestId, int requestNumber, byte[] body, bool final)
		{
			IWebRequest request;
			lock (requestsLock)
			{
				requests.TryGetValue (requestId, out request);
			}

			if (request != null
				&& request.RequestNumber == requestNumber) {

				if (final) {
					lock (requestsLock) {
						requests.Remove (requestId);
					}
					request.Process ((IWebResponse)request);
				} else {
					request.AddBodyPart (body);
				}
			}

		}

		public void Process (ulong requestId, int requestNumber)
		{
			//			Console.WriteLine ("Remove ReqId={0}", requestId);
			lock (requestsLock) {
				requests.Remove (requestId);
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
		public extern void SendOutput (ulong requestId, int requestNumber, byte[] data, int len);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void EndRequest (ulong requestId, int requestNumber, int appStatus);

	}
}



