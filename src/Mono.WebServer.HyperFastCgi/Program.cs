//using System;
//using System.Threading;
//using System.Diagnostics;
//using Mono.WebServer;
//
//namespace Mono.WebServer.HyperFastCgi
//{
//	class MainClass
//	{
//		private static ApplicationServer appServer;
//
//		public static ApplicationHost GetAppHost()
//		{
//			return (ApplicationHost)appServer.GetApplicationForPath ("ssbench3", 81, "/", false).AppHost;
//		}
//
//		public static void Main (string[] args)
//		{
//			int worker, completion;
//			int mworker, mcompletion;
//			ThreadPool.SetMinThreads (20, 8);
//			ThreadPool.GetMinThreads (out worker, out completion);
//			ThreadPool.GetMaxThreads (out mworker, out mcompletion);
//			Console.WriteLine ("Hyper FastCgi ProcessId={0}, nThreads={1},{2}",Process.GetCurrentProcess().Id,worker,completion);
//
//			appServer = new ApplicationServer (new WebSource (), Environment.CurrentDirectory);
//
//			appServer.AddApplication ("ssbench3", 81, "/", "/var/www/nginx-mono");
//
//			VPathToHost vapp = appServer.GetApplicationForPath ("ssbench3", 81, "/", false);
//
//			/*Server server = new Server ();
//
//			server.Start ();*/
//			GetAppHost ().Start ();
//
//			Console.ReadLine ();
//
//			//server.Shutdown ();
//			GetAppHost ().Shutdown ();
//		}
//	}
//}
