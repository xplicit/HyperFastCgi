using System;
using HyperFastCgi.Interfaces;
using System.Collections.Generic;
using HyperFastCgi.Configuration;

namespace HyperFastCgi.Transports
{
	public abstract class BaseAppHostTransport : MarshalByRefObject, IApplicationHostTransport
	{
		Dictionary<ulong, IWebRequest> requests = new Dictionary<ulong, IWebRequest> ();
		object requestsLock = new object ();
		IApplicationHost appHost;
		MultiThreadingOption mt;

		public IApplicationHost AppHost {
			get { return appHost; }
		}

		public MultiThreadingOption MultiThreading {
			get { return mt; }
		}

		protected virtual void OnHostUnload (IApplicationHost host, bool isShutdown)
		{

		}

		#region IApplicationHostTransport implementation

		public virtual void Configure (IApplicationHost host, object transportConfig)
		{
			this.appHost = host;
			host.HostUnload += (sender, e) => OnHostUnload (
				(IApplicationHost)sender, 
				e.IsShutdown);

			TransportConfig config = transportConfig as TransportConfig;

			if (config != null) {
				mt=config.MultiThreading;
			}
		}

		public virtual void CreateRequest (ulong requestId, int requestNumber)
		{
//			Console.WriteLine ("reqN={0}", requestNumber);
			IWebRequest req=AppHost.CreateRequest (requestId, requestNumber, null);

			lock (requestsLock) {
				requests.Add (requestId, req);
			}

		}

		public virtual void AddServerVariable (ulong requestId, int requestNumber, string name, string value)
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

		public virtual void AddHeader (ulong requestId, int requestNumber, string name, string value)
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

		public virtual void HeadersSent (ulong requestId, int requestNumber)
		{

		}

		public virtual void AddBodyPart (ulong requestId, int requestNumber, byte[] body, bool final)
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

		public virtual void Process (ulong requestId, int requestNumber)
		{
			//			Console.WriteLine ("Remove ReqId={0}", requestId);
			lock (requestsLock) {
				requests.Remove (requestId);
			}
		}

		public abstract void SendOutput (ulong requestId, int requestNumber, byte[] data, int len);

		public abstract void EndRequest (ulong requestId, int requestNumber, int appStatus);

		#endregion
	}
}

