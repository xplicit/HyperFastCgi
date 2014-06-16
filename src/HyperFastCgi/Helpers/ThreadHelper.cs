using System;
using System.Threading;
using HyperFastCgi.Helpers.Logging;

namespace HyperFastCgi.Helpers
{
	public class ThreadHelper
	{
		public static void SetThreads(int minWorkerThreads, int minIOThreads, int maxWorkerThreads, int maxIOThreads)
		{
			if (minWorkerThreads ==0 &&  minIOThreads ==0 && maxWorkerThreads ==0 && maxIOThreads == 0)
				return;

			if ((maxWorkerThreads != 0 && maxWorkerThreads < minWorkerThreads) || (maxIOThreads != 0 && maxIOThreads < minIOThreads))
				throw new ArgumentException ("min threads must not be greater max threads");
			int minwth, minioth, maxwth, maxioth;

			ThreadPool.GetMinThreads (out minwth, out minioth);
			ThreadPool.GetMaxThreads (out maxwth, out maxioth);

			if (minWorkerThreads > minwth)
				minwth = minWorkerThreads;
			if (minIOThreads>minioth)
				minioth = minIOThreads;
			if (maxWorkerThreads != 0)
				maxwth = maxWorkerThreads;
			if (maxIOThreads != 0)
				maxioth = maxIOThreads;
			if (maxwth < minwth)
				maxwth = minwth;
			if (maxioth < minioth)
				maxioth = minioth;

			if (!ThreadPool.SetMaxThreads (maxwth, maxioth) ||
				!ThreadPool.SetMinThreads (minwth, minioth))
				throw new Exception ("Could not set threads");

			Logger.Write (LogLevel.Debug, "Threadpool minw={0},minio={1},maxw={2},maxio={3}", minwth, minioth, maxwth, maxioth);
		}
	}
}

