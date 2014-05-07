using System;
using System.Collections.Generic;
using Mono.WebServer.HyperFastCgi.FastCgiProtocol;

namespace Mono.WebServer.HyperFastCgi.Transport
{
	public class FastCgiRecordQueue
	{
		public uint listenerTag;
		public int processing; 
		public Queue<Record> queue=new Queue<Record> ();
		public object queueLock=new object (); 

		public FastCgiRecordQueue ()
		{
		}

	}
}

