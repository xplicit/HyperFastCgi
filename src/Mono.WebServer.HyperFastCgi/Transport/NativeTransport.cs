using System;
using Mono.WebServer.HyperFastCgi.Interfaces;
using System.Collections.Generic;
using Mono.WebServer.HyperFastCgi.Requests;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Mono.WebServer.HyperFastCgi.AspNetServer;

namespace Mono.WebServer.HyperFastCgi.Transport
{
	public class NativeTransport : INativeTransport
	{
		Dictionary<long, NativeRequest> requests = new Dictionary<long, NativeRequest> ();

		public IApplicationHost AppHost {
			get;
			set;
		}

		#region INativeTransport implementation

		public void CreateRequest (long requestId, int requestNumber)
		{
//			Console.WriteLine ("Add ReqId={0}", requestId);

			requests.Add (requestId, new NativeRequest (requestId, requestNumber));

		}

		public void AddServerVariable (long requestId, int requestNumber, string name, string value)
		{
			NativeRequest request;

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

		public void AddHeader (long requestId, int requestNumber, string name, string value)
		{
			NativeRequest request;

			if (requests.TryGetValue (requestId, out request)
				&& request.RequestNumber == requestNumber) {
				request.AddHeader (name, value);
			}
//			Console.WriteLine ("header: {0}={1}",name, value);
		}

		public void HeadersSent (long requestId, int requestNumber)
		{

		}

		public void AddBodyPart (long requestId, int requestNumber, byte[] body, bool final)
		{
			NativeRequest request;

			if (requests.TryGetValue (requestId, out request)
				&& request.RequestNumber == requestNumber) {

				if (final) {
					requests.Remove (requestId);
					AspNetNativeWebRequest req = new AspNetNativeWebRequest (request, AppHost, this);
					req.Process (req);
				} else {
					request.AddInputData (body);
				}
			}

		}

		public void Process (long requestId, int requestNumber)
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
		public extern static void SendOutput (long requestId, int requestNumber, byte[] data, int len);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void EndRequest (long requestId, int requestNumber, int appStatus);

		[DllImport("libnative", EntryPoint="bridge_register_icall")]
		public extern static void RegisterIcall ();

		public NativeTransport ()
		{
		}

	}
}

