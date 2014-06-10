using System;
using HyperFastCgi.Interfaces;

namespace HyperFastCgi.Transports
{
	public class ManagedFastCgiListenerTransport : BaseManagedListenerTransport
	{
		#region implemented abstract members of BaseManagedListenerTransport
		public override void CreateRequest (TransportRequest req)
		{
			req.Transport.CreateRequest (req.Hash, req.RequestNumber);
		}
		public override void AddHeader (TransportRequest req, string name, string value)
		{
			req.Transport.AddHeader (req.Hash, req.RequestNumber, name, value);
		}
		public override void AddServerVariable (TransportRequest req, string name, string value)
		{
			req.Transport.AddServerVariable (req.Hash, req.RequestNumber, name, value);
		}
		public override void HeadersSent (TransportRequest req)
		{
			req.Transport.HeadersSent (req.Hash, req.RequestNumber);
		}
		public override void AddBodyPart (TransportRequest req, byte[] body, bool final)
		{
			req.Transport.AddBodyPart (req.Hash, req.RequestNumber, body, final);
		}
		public override void Process (TransportRequest req)
		{
			req.Transport.Process (req.Hash, req.RequestNumber);
		}
		public override bool IsHostFound (TransportRequest req)
		{
			return req.Transport != null;
		}
		public override void GetRoute (TransportRequest req, string vhost, int vport, string vpath)
		{
			IApplicationHost host = Listener.Server.GetRoute (vhost, vport, vpath);

			if (host != null) {
				req.Transport = host.AppHostTransport;
			}
		}
		#endregion
	}
}

