using System;

namespace HyperFastCgi.Interfaces.Events
{
	[Serializable]
	public class HostUnloadEventArgs : EventArgs
	{
		public bool IsShutdown { get; set;}
	}
}

