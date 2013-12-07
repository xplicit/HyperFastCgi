using System;

namespace Mono.WebServer
{
	public class DomainReloadEventArgs : EventArgs
	{
		public DomainReloadEventArgs ()
		{
		}

		public VPathToHost VApp {
			get;
			set;
		}
	}
}

