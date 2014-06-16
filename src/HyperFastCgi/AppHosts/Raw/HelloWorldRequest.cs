using System;
using System.Text;
using HyperFastCgi.Interfaces;
using HyperFastCgi.Helpers;

namespace HyperFastCgi.AppHosts.Raw
{
	public class HelloWorldRequest : BaseRawRequest
	{
		public override void Process(IWebResponse response)
		{
			//response.Send(Encoding.ASCII.GetBytes(TestResponse.Header));
			Status = 200;
			StatusDescription = "OK";
			ResponseHeaders.Add("Content-Type","text/html; charset=utf-8");
			response.Send(Encoding.ASCII.GetBytes(TestResponse.Response));
			response.CompleteResponse ();
		}
	}
}

