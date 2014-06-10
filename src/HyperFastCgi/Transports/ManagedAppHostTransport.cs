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
	public class ManagedAppHostTransport : MarshalByRefObject, IApplicationHostTransport
	{
		Dictionary<ulong, IWebRequest> requests = new Dictionary<ulong, IWebRequest> ();
		object requestsLock = new object ();
		IApplicationHost appHost;
		bool isUnload;

		public IApplicationHost AppHost {
			get { return appHost;}
		}

		#region INativeTransport implementation

		public void Configure (IApplicationHost host, object config)
		{
			this.appHost = host;
			host.HostUnload += OnHostUnload;
		}

		void OnHostUnload (object sender, HyperFastCgi.Interfaces.Events.HostUnloadEventArgs e)
		{
			isUnload = true;
		}

		public void CreateRequest (ulong requestId, int requestNumber)
		{
//			Console.WriteLine ("CreateRequest hash={0}, reqN={1}", requestId, requestNumber);

			IWebRequest req=AppHost.CreateRequest (requestId, requestNumber, null);

			lock (requestsLock) {
				requests.Add (requestId, req);
			}
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
//			Console.WriteLine ("AddBodyPart hash={0}, reqN={1}, final={2}", requestId, requestNumber, final);
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

		public void SendOutput (ulong requestId, int requestNumber, byte[] data, int len)
		{
			if (!isUnload) {
				appHost.ListenerTransport.SendOutput (requestId, requestNumber, data, len);
			}
		}

		public void EndRequest (ulong requestId, int requestNumber, int appStatus)
		{
			if (!isUnload) {
				appHost.ListenerTransport.EndRequest (requestId, requestNumber, appStatus);
			}
		}
	}
}


