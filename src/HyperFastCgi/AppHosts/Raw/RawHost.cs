using System;
using HyperFastCgi.Interfaces;
using HyperFastCgi.Configuration;
using System.Reflection.Emit;
using HyperFastCgi.Logging;

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

		public override void Configure (object appHostConfig, object webAppConfig,
			IApplicationServer server, 
			IListenerTransport listenerTransport, 
			Type appHostTransportType, object transportConfig)
		{
			RawHostConfig config = appHostConfig as RawHostConfig;

			if (config != null) {
				Logger.Level = config.Log.Level;
				Logger.WriteToConsole = config.Log.WriteToConsole;

				requestType = Type.GetType(config.RequestType);
				if (requestType == null) {
					Logger.Write(LogLevel.Error, "Couldn't find type '{0}'", config.RequestType);
					throw new ArgumentException ("appHostConfig.Type");
				}
				CreateRequestInstance = CreateDynamicMethod (requestType);
			}

			base.Configure (appHostConfig, webAppConfig, server, listenerTransport, appHostTransportType, transportConfig);
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

