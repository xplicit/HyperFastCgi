using System;
using Mono.WebServer.HyperFastCgi.Interfaces;
using System.Collections.Generic;
using Mono.WebServer.HyperFastCgi.Requests;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Mono.WebServer.HyperFastCgi.AspNetServer;
using System.Threading;
using System.Text;
#if !NET_2_0
using System.Threading.Tasks;
#endif 

namespace Mono.WebServer.HyperFastCgi.Transport
{
	public class NativeTransport : INativeTransport
	{
		Dictionary<ulong, IWebRequest> requests = new Dictionary<ulong, IWebRequest> ();
		IApplicationHost appHost;

		public IApplicationHost AppHost {
			get { return appHost;}
		}

		#region INativeTransport implementation

		public void Configure (IApplicationHost host, object config)
		{
			this.appHost = host;
			RegisterHost (host.VPath, host.Path);
		}

		public void CreateRequest (ulong requestId, int requestNumber)
		{
			IWebRequest req=AppHost.CreateRequest (requestId, requestNumber, null);

			requests.Add (requestId, req);
		}

		public void AddServerVariable (ulong requestId, int requestNumber, string name, string value)
		{
			IWebRequest request;

			try
			{
			if (requests.TryGetValue (requestId, out request)
			    && request.RequestNumber == requestNumber) {
				request.AddServerVariable (name, value);
			}
			} catch (Exception ex) {
				Console.WriteLine ("ex={0}", ex.ToString ());
			}
//			Console.WriteLine ("svar: {0}={1}",name, value);
		}

		public void AddHeader (ulong requestId, int requestNumber, string name, string value)
		{
			IWebRequest request;

			if (requests.TryGetValue (requestId, out request)
				&& request.RequestNumber == requestNumber) {
				request.AddHeader (name, value);
			}
//			Console.WriteLine ("header: {0}={1}",name, value);
		}

		public void HeadersSent (ulong requestId, int requestNumber)
		{

		}

		public void AddBodyPart (ulong requestId, int requestNumber, byte[] body, bool final)
		{
			IWebRequest request;

			if (requests.TryGetValue (requestId, out request)
				&& request.RequestNumber == requestNumber) {

				if (final) {
					requests.Remove (requestId);
//					AspNetNativeWebRequest req = new AspNetNativeWebRequest (request, AppHost, this);
//					req.Process (req);
//					ThreadPool.QueueUserWorkItem ( _ => req.Process (req));
					//ThreadPool.UnsafeQueueUserWorkItem (ProcessInternal,req);
//					Task.Factory.StartNew ( () => {
					request.Process ((IWebResponse)request);
//					});
//					ThreadPool.QueueUserWorkItem (_ => {
//						for(int i=0;i<1000000;i++)
//						{
//							int j=i*i;
//						}
//						SendOutput (request.RequestId, request.RequestNumber, header, header.Length);
//						SendOutput (request.RequestId, request.RequestNumber, content, content.Length);
//						EndRequest (request.RequestId, request.RequestNumber, 0);
//					});
				} else {
					request.AddBodyPart (body);
				}
			}

		}

		private void ProcessInternal(object state)
		{
			AspNetNativeWebRequest req=(AspNetNativeWebRequest)state;
			req.Process (req);
		}

		public void Process (ulong requestId, int requestNumber)
		{
//			Console.WriteLine ("Remove ReqId={0}", requestId);
			requests.Remove (requestId);
		}
		#endregion

		//[DllImport("libnative")]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void RegisterHost (string virtualPath, string path);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void RegisterTransport (Type transportType);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void SendOutput (ulong requestId, int requestNumber, byte[] data, int len);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void EndRequest (ulong requestId, int requestNumber, int appStatus);

		[DllImport("libhfc-native", EntryPoint="bridge_register_icall")]
		public extern static void RegisterIcall ();

		byte[] header=Encoding.ASCII.GetBytes(TestResponse.Header);
		byte[] content=Encoding.ASCII.GetBytes(TestResponse.Response);

		public NativeTransport ()
		{

		}

	}
}

