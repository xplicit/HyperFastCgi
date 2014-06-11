using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HyperFastCgi.Transports
{
	public class CombinedFastCgiListenerTransport : BaseManagedListenerTransport
	{
		#region implemented abstract members of BaseManagedListenerTransport
		public override void CreateRequest (TransportRequest req)
		{
			AppHostTransportCreateRequest (req.Host, req.Hash, req.RequestNumber);
		}
		public override void AddHeader (TransportRequest req, string name, string value)
		{
			AppHostTransportAddHeader (req.Host, req.Hash, req.RequestNumber, name, value);
		}
		public override void AddServerVariable (TransportRequest req, string name, string value)
		{
			AppHostTransportAddServerVariable (req.Host, req.Hash, req.RequestNumber, name, value);
		}
		public override void HeadersSent (TransportRequest req)
		{
			AppHostTransportHeadersSent (req.Host, req.Hash, req.RequestNumber);
		}
		public override void AddBodyPart (TransportRequest req, byte[] body, bool final)
		{
			AppHostTransportAddBodyPart (req.Host, req.Hash, req.RequestNumber, body, final);
		}
		public override void Process (TransportRequest req)
		{
			AppHostTransportProcess (req.Host, req.Hash, req.RequestNumber);
		}
		public override bool IsHostFound (TransportRequest req)
		{
			return req.Host != IntPtr.Zero;
		}
		public override void GetRoute (TransportRequest req, string vhost, int vport, string vpath)
		{
			req.Host = GetRoute (vhost, vport, vpath);
		}
		#endregion

		#region override callbacks
		//these overrides are required here only becase native code in bridge_register_transport 
		//can't find methods in base class. When native code will be able to find method in base class
		//these overrides may be safelly removed
		public override void SendOutput (ulong hash, int requestNumber, byte[] data, int length)
		{
			base.SendOutput (hash, requestNumber, data, length);
		}

		public override void EndRequest (ulong hash, int requestNumber, int appStatus)
		{
			base.EndRequest (hash, requestNumber, appStatus);
		}
		#endregion

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern IntPtr GetRoute (string vhost, int vport, string vpath);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void AppHostTransportCreateRequest (IntPtr host, ulong requestId, int requestNumber);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void AppHostTransportAddServerVariable (IntPtr host, ulong requestId, int requestNumber, string name, string value);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void AppHostTransportAddHeader (IntPtr host, ulong requestId, int requestNumber, string name, string value);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void AppHostTransportHeadersSent (IntPtr host, ulong requestId, int requestNumber);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void AppHostTransportAddBodyPart (IntPtr host, ulong requestId, int requestNumber, byte[] body, bool final);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void AppHostTransportProcess (IntPtr host, ulong requestId, int requestNumber);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void RegisterTransport ();

		[DllImport("libhfc-native", EntryPoint="domain_bridge_register_icall")]
		public extern static void RegisterIcall ();

		delegate void HideFromJit();    

		static CombinedFastCgiListenerTransport ()
		{
			CombinedFastCgiListenerTransport.RegisterIcall ();
		}

		public CombinedFastCgiListenerTransport()
		{
			HideFromJit d=RegisterTransport;
			d ();
		}

	}
}

