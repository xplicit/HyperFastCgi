//
// server.cs: Web Server that uses ASP.NET hosting
//
// Authors:
//   Sergey Zhukov 
//   Brian Nickel (brian.nickel@gmail.com)
//   Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) 2002,2003 Ximian, Inc (http://www.ximian.com)
// (C) Copyright 2004 Novell, Inc. (http://www.novell.com)
// (C) Copyright 2007 Brian Nickel
// (C) Copyright 2013 Sergey Zhukov
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Web.Hosting;
using Mono.WebServer;
using System.Configuration;
using Mono.Unix;
using Mono.Unix.Native;
using Mono.WebServer.HyperFastCgi.Logging;
using Mono.WebServer.HyperFastCgi.Sockets;
using System.Threading;
using Mono.WebServer.HyperFastCgi.ApplicationServers;
using Mono.WebServer.HyperFastCgi.Transport;
using Mono.WebServer.HyperFastCgi.Listener;
using System.Net.Sockets;
using System.Collections.Generic;
using Mono.WebServer.HyperFastCgi.Config;
using Mono.WebServer.HyperFastCgi.Interfaces;

namespace Mono.WebServer.HyperFastCgi
{
	public class MainClass
	{
		static void ShowVersion ()
		{
			Assembly assembly = Assembly.GetExecutingAssembly ();
			string version = assembly.GetName ().Version.ToString ();
			object att;

			att = assembly.GetCustomAttributes (
				typeof(AssemblyCopyrightAttribute), false) [0];
			string copyright =
				((AssemblyCopyrightAttribute)att).Copyright;

			att = assembly.GetCustomAttributes (
				typeof(AssemblyDescriptionAttribute), false) [0];
			string description =
				((AssemblyDescriptionAttribute)att).Description;

			Console.WriteLine ("{0} {1}\n(c) {2}\n{3}",
				Path.GetFileName (assembly.Location), version,
				copyright, description);
		}

		static void ShowHelp ()
		{
			string name = Path.GetFileName (
				              Assembly.GetExecutingAssembly ().Location);

			ShowVersion ();
			Console.WriteLine ();
			Console.WriteLine ("Usage is:\n");
			Console.WriteLine ("    {0} [...]", name);
			Console.WriteLine ();
			configmanager.PrintHelp ();
		}

		private static ApplicationServer appserver;
		private static ConfigurationManager configmanager;

		private static AddressFamily sockType;
		private static string address; 
		private static ushort port=0;
		private static bool keepAlive; 
		private static bool useThreadPool;

		public static int Main (string[] args)
		{
			// Load the configuration file stored in the
			// executable's resources.
			configmanager = new ConfigurationManager (
				typeof(Server).Assembly,
				"ConfigurationManager.xml");

			configmanager.LoadCommandLineArgs (args);

			// Show the help and exit.
			if ((bool)configmanager ["help"] ||
			    (bool)configmanager ["?"]) {
				ShowHelp ();
				return 0;
			}

			// Show the version and exit.
			if ((bool)configmanager ["version"]) {
				ShowVersion ();
				return 0;
			}

			string config = (string) configmanager ["config"];

			if (config == null) {
				Console.WriteLine ("You must pass /config=<filename> option. See 'help' for more info");
				return 1;
			}


			try {
				string config_file = (string)
				                     configmanager ["configfile"];
				if (config_file != null)
					configmanager.LoadXmlConfig (
						config_file);
			} catch (ApplicationException e) {
				Console.WriteLine (e.Message);
				return 1;
			} catch (System.Xml.XmlException e) {
				Console.WriteLine (
					"Error reading XML configuration: {0}",
					e.Message);
				return 1;
			}

			try {
				string log_level = (string)
				                   configmanager ["loglevels"];

				if (log_level != null)
					Logger.Level = (LogLevel)
					               Enum.Parse (typeof(LogLevel),
						log_level);
			} catch {
				Console.WriteLine ("Failed to parse log levels.");
				Console.WriteLine ("Using default levels: {0}",
					Logger.Level);
			}

			// Enable console logging during Main ().
			Logger.WriteToConsole = true;

			try {
				string log_file = (string)
				                  configmanager ["logfile"];

				if (log_file != null)
					Logger.Open (log_file);
			} catch (Exception e) {
				Logger.Write (LogLevel.Error,
					"Error opening log file: {0}",
					e.Message);
				Logger.Write (LogLevel.Error,
					"Events will not be logged.");
			}

			Logger.Write (LogLevel.Debug,
				Assembly.GetExecutingAssembly ().GetName ().Name);

			// Socket strings are in the format
			// "type[:ARG1[:ARG2[:...]]]".
			string socket_type = configmanager ["socket"] as string;
			if (socket_type == null)
				socket_type = "pipe";

			string[] socket_parts = socket_type.Split (
				                         new char [] { ':' }, 3);

			switch (socket_parts [0].ToLower ()) {
			// The FILE sockets is of the format
			// "file[:PATH]".
			case "unix":
			case "file":
				if (socket_parts.Length == 2)
					configmanager ["filename"] =
						socket_parts [1];

				string path = (string)configmanager ["filename"];

				sockType = AddressFamily.Unix;
				address = path;

				Logger.Write (LogLevel.Debug,
					"Listening on file: {0}",	path);
				break;

			// The TCP socket is of the format
			// "tcp[[:ADDRESS]:PORT]".
			case "tcp":
				if (socket_parts.Length > 1)
					configmanager ["port"] = socket_parts [
						socket_parts.Length - 1];

				if (socket_parts.Length == 3)
					configmanager ["address"] =
						socket_parts [1];

				//ushort port;
				try {
					port = (ushort)configmanager ["port"];
				} catch (ApplicationException e) {
					Logger.Write (LogLevel.Error, e.Message);
					return 1;
				}

				string address_str =
					(string)configmanager ["address"];

				try {
					IPAddress.Parse (address_str);
				} catch {
					Logger.Write (LogLevel.Error,
						"Error in argument \"address\". \"{0}\" cannot be converted to an IP address.",
						address_str);
					return 1;
				}

				sockType = AddressFamily.InterNetwork;
				address = address_str;

				Logger.Write (LogLevel.Debug,
					"Listening on port: {0}", port);
				Logger.Write (LogLevel.Debug,
					"Listening on address: {0}", address_str);
				break;

			default:
				Logger.Write (LogLevel.Error,
					"Error in argument \"socket\". \"{0}\" is not a supported type. Use \"pipe\", \"tcp\" or \"unix\".",
					socket_parts [0]);
				return 1;
			}

			string root_dir = configmanager ["root"] as string;
			if (root_dir != null && root_dir.Length != 0) {
				try {
					Environment.CurrentDirectory = root_dir;
				} catch (Exception e) {
					Logger.Write (LogLevel.Error,
						"Error: {0}", e.Message);
					return 1;
				}
			}

			root_dir = Environment.CurrentDirectory;
			bool auto_map = false; //(bool) configmanager ["automappaths"];

			appserver = new ApplicationServer (typeof(ApplicationHost), root_dir);
			appserver.Verbose = (bool)configmanager ["verbose"];
			appserver.DomainReloadEvent +=DomainReloadEventHandler;

			string applications = (string)
			                      configmanager ["applications"];
			string app_config_file;
			string app_config_dir;
			List<WebAppConfig> webapps = new List<WebAppConfig> ();

			try {
				app_config_file = (string)
				                  configmanager ["appconfigfile"];
				app_config_dir = (string)
				                 configmanager ["appconfigdir"];
			} catch (ApplicationException e) {
				Logger.Write (LogLevel.Error, e.Message);
				return 1;
			}

			if (applications != null) {
				webapps.AddRange (ConfigUtils.GetApplicationsFromCommandLine (applications));
			}

			if (app_config_file != null) {
				webapps.AddRange (ConfigUtils.GetApplicationsFromConfigFile (app_config_file));
			}

			if (app_config_dir != null) {
				webapps.AddRange (ConfigUtils.GetApplicationsFromConfigDirectory (app_config_dir));
			}

			if (applications == null && app_config_dir == null &&
			    app_config_file == null && !auto_map) {
				Logger.Write (LogLevel.Error,
					"There are no applications defined, and path mapping is disabled.");
				Logger.Write (LogLevel.Error,
					"Define an application using /applications, /appconfigfile, /appconfigdir");
				/*				
				Logger.Write (LogLevel.Error,
					"or by enabling application mapping with /automappaths=True.");
				*/
				return 1;
			}

			Logger.Write (LogLevel.Debug, "Root directory: {0}", root_dir);

			keepAlive = (bool)configmanager ["keepalive"];
			useThreadPool = (bool)configmanager ["usethreadpool"];

			string[] minThreads = ((string)configmanager ["minthreads"]).Split(',');
			string[] maxThreads = ((string)configmanager ["maxthreads"]).Split(',');
			int mintw=0, mintio=0, maxtw=0, maxtio=0;

			Int32.TryParse (minThreads [0], out mintw);
			if (minThreads.Length > 1)
				Int32.TryParse (minThreads [1], out mintio);

			Int32.TryParse (maxThreads [0], out maxtw);
			if (maxThreads.Length > 1)
				Int32.TryParse (maxThreads [1], out maxtio);

			SetThreads (mintw, mintio, maxtw, maxtio);


//			server.MaxConnections = (ushort)
//			                        configmanager ["maxconns"];
//			server.MaxRequests = (ushort)
//			                     configmanager ["maxreqs"];
//			server.MultiplexConnections = (bool)
//			                              configmanager ["multiplex"];

//			Logger.Write (LogLevel.Debug, "Max connections: {0}",
//				server.MaxConnections);
//			Logger.Write (LogLevel.Debug, "Max requests: {0}",
//				server.MaxRequests);
//			Logger.Write (LogLevel.Debug, "Multiplex connections: {0}",
//				server.MultiplexConnections);

			bool stopable = (bool)configmanager ["stopable"];
			Logger.WriteToConsole = (bool)configmanager ["printlog"];
//			host.LogLevel = Logger.Level;
//			host.LogToConsole = Logger.WriteToConsole;
//			host.AddTrailingSlash = (bool)configmanager ["addtrailingslash"];

			SimpleApplicationServer srv = new SimpleApplicationServer (root_dir);

			List<ConfigInfo> listenerConfigs = ConfigUtils.GetConfigsFromFile (config, "listener", typeof(ListenerConfig));
			if (listenerConfigs.Count != 1) {
				Console.WriteLine ("Only one listener are supported");
				return 1;
			}

			IWebListener listener = (IWebListener)Activator.CreateInstance(listenerConfigs[0].Type);
			listener.Configure (srv, listenerConfigs[0].Config);

			foreach (WebAppConfig app in webapps) {
				srv.CreateApplicationHost (app.VHost, app.VPort, app.VPath, app.RealPath,
					listener.Transport, listener.AppHostTransportType, null);
			}
			listener.Listen (sockType, address, port);

			configmanager = null;

			if (stopable) {
				Console.WriteLine (
					"Hit Return to stop the server.");
				Console.ReadLine ();
//				host.Shutdown ();
			} else {
				UnixSignal[] signals = new UnixSignal[] { 
					new UnixSignal (Signum.SIGINT), 
					new UnixSignal (Signum.SIGTERM), 
				};

				// Wait for a unix signal
				for (bool exit = false; !exit;) {
					int id = UnixSignal.WaitAny (signals);

					if (id >= 0 && id < signals.Length) {
						if (signals [id].IsSet)
							exit = true;
					}
				}
			}

			return 0;
		}

		static void DomainReloadEventHandler(object sender,DomainReloadEventArgs args)
		{
//			ApplicationHost host = args.VApp.AppHost as ApplicationHost;
//
//			host.Start (sockType, address, port, keepAlive, useThreadPool);
		}

		static void SetThreads(int minWorkerThreads, int minIOThreads, int maxWorkerThreads, int maxIOThreads)
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
