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
	/// <summary>
	/// Native application host transport for FastCgi NativeListener
	/// </summary>
	public class NativeTransport : BaseAppHostTransport
	{
		#region BaseAppHostTransport overrides

		protected override void OnHostUnload (IApplicationHost host, bool isShutdown)
		{
			Logger.Write (LogLevel.Debug, "Unloading ApplicationHost domain, isShutdown={0}", isShutdown);

			UnregisterHost (
				host.VHost,
				host.VPort,
				host.VPath
			);
		}
		public override void Configure (IApplicationHost host, object config)
		{
			Logger.Write (LogLevel.Debug, "Configuring ApplicationHost");

			base.Configure (host, config);

			RegisterHost (host.VHost, host.VPort, host.VPath, host.Path);
		}

		#endregion

		//[DllImport("libnative")]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void RegisterHost (string host, int port, string virtualPath, string path);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void UnregisterHost (string host, int port, string virtualPath);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern static void RegisterTransport (Type transportType);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern override void SendOutput (ulong requestId, int requestNumber, byte[] data, int len);

		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern override void EndRequest (ulong requestId, int requestNumber, int appStatus);

		[DllImport("libhfc-native", EntryPoint="bridge_register_icall")]
		public extern static void RegisterIcall ();

		delegate void HideFromJit(Type t);    
		private static HideFromJit d=RegisterTransport;

		static NativeTransport ()
		{
			Logger.Write (LogLevel.Debug, "Register native transport");
			NativeTransport.RegisterIcall ();
			d (typeof(NativeTransport));
		}

	}
}

