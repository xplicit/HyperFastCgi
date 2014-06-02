using System;
using HyperFastCgi.Interfaces;
using HyperFastCgi.Configuration;
using System.Reflection.Emit;

namespace HyperFastCgi.AppHosts.Raw
{
	[Config(typeof(RawHostConfig))]
	public class RawHost : AppHostBase
	{
		#region implemented abstract members of AppHostBase
		private CreateInstanceDelegate CreateRequestInstance;
		private Type requestType;

		public override IWebRequest CreateRequest (ulong requestId, int requestNumber, object arg)
		{
			BaseRawRequest request = CreateRequestInstance ();
			request.Configure (requestId, requestNumber, this);

			return request;
		}

		public override IWebResponse GetResponse (IWebRequest request, object arg)
		{
			return (IWebResponse)request;
		}

		public override void ProcessRequest (IWebRequest request)
		{
			request.Process ((IWebResponse)request);
		}

		#endregion

		public override void Configure (IApplicationServer server, IListenerTransport listenerTransport, 
			Type appHostTransportType, object transportConfig, object appHostConfig)
		{
			RawHostConfig config = appHostConfig as RawHostConfig;

			if (config != null) {
				requestType = Type.GetType(config.RequestType);
				CreateRequestInstance = CreateDynamicMethod (requestType);
			}

			base.Configure (server, listenerTransport, appHostTransportType, transportConfig, appHostConfig);
		} 


		private delegate BaseRawRequest CreateInstanceDelegate ();

		private CreateInstanceDelegate CreateDynamicMethod(Type requestType)
		{
			DynamicMethod createInstance = new DynamicMethod(
				"CreateInstance", 
				typeof(BaseRawRequest), 
				new Type[]{}, 
				requestType.Module);

			ILGenerator il = createInstance.GetILGenerator();
			il.Emit(OpCodes.Newobj, requestType.GetConstructor(new Type[0]));
			il.Emit(OpCodes.Ret);

			return (CreateInstanceDelegate)createInstance.CreateDelegate (typeof(CreateInstanceDelegate));
		}



	}
}

