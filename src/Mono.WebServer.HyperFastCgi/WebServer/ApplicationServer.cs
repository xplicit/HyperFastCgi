//
// ApplicationServer.cs
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Lluis Sanchez Gual (lluis@ximian.com)
//
// Copyright (c) Copyright 2002-2007 Novell, Inc
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
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Web;
using System.Web.Hosting;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Mono.WebServer
{
	// ApplicationServer runs the main server thread, which accepts client
	// connections and forwards the requests to the correct web application.
	// ApplicationServer takes an WebSource object as parameter in the
	// constructor. WebSource provides methods for getting some objects
	// whose behavior is specific to XSP or mod_mono.
	// Each web application lives in its own application domain, and incoming
	// requests are processed in the corresponding application domain.
	// Since the client Socket can't be passed from one domain to the other, the
	// flow of information must go through the cross-app domain channel.
	// For each application two objects are created:
	// 1) a IApplicationHost object is created in the application domain
	// 2) a IRequestBroker is created in the main domain.
	//
	// The IApplicationHost is used by the ApplicationServer to start the
	// processing of a request in the application domain.
	// The IRequestBroker is used from the application domain to access
	// information in the main domain.
	//
	// The complete sequence of servicing a request is the following:
	//
	// 1) The listener accepts an incoming connection.
	// 2) An Worker object is created (through the WebSource), and it is
	//    queued in the thread pool.
	// 3) When the Worker's run method is called, it registers itself in
	//    the application's request broker, and gets a request id. All this is
	//    done in the main domain.
	// 4) The Worker starts the request processing by making a cross-app domain
	//    call to the application host. It passes as parameters the request id
	//    and other information already read from the request.
	// 5) The application host executes the request. When it needs to read or
	//    write request data, it performs remote calls to the request broker,
	//    passing the request id provided by the Worker.
	// 6) When the request broker receives a call from the application host,
	//    it locates the Worker registered with the provided request id and
	//    forwards the call to it.
	public class ApplicationServer : MarshalByRefObject
	{
		Type applicationHostType;
		bool verbose;
		bool single_app;
		string physicalRoot;
		// This is much faster than hashtable for typical cases.
		ArrayList vpathToHost = new ArrayList ();

		public bool SingleApplication {
			get { return single_app; }
			set { single_app = value; }
		}

		public IApplicationHost AppHost {
			get { return ((VPathToHost)vpathToHost [0]).AppHost; }
			set { ((VPathToHost)vpathToHost [0]).AppHost = value; }
		}

		public string PhysicalRoot {
			get { return physicalRoot; }
		}

		public bool Verbose {
			get { return verbose; }
			set { verbose = value; }
		}

		#region events
		public EventHandler<DomainReloadEventArgs> DomainReloadEvent;

		#endregion

		public ApplicationServer (Type applicationHostType, string physicalRoot)
		{
			if (applicationHostType == null)
				throw new ArgumentNullException ("applicationHostType");
			if (physicalRoot == null || physicalRoot.Length == 0)
				throw new ArgumentNullException ("physicalRoot");
			
			this.applicationHostType = applicationHostType;
			this.physicalRoot = physicalRoot;
		}

		public void AddApplication (string vhost, int vport, string vpath, string fullPath)
		{
			char dirSepChar = Path.DirectorySeparatorChar;
			if (fullPath != null && !fullPath.EndsWith (dirSepChar.ToString ()))
				fullPath += dirSepChar;
			
			// TODO - check for duplicates, sort, optimize, etc.
			if (verbose && !single_app) {
				Console.WriteLine ("Registering application:");
				Console.WriteLine ("    Host:          {0}", (vhost != null) ? vhost : "any");
				Console.WriteLine ("    Port:          {0}", (vport != -1) ?
						  vport.ToString () : "any");

				Console.WriteLine ("    Virtual path:  {0}", vpath);
				Console.WriteLine ("    Physical path: {0}", fullPath);
			}

			vpathToHost.Add (new VPathToHost (vhost, vport, vpath, fullPath));
		}

		public void AddApplicationsFromConfigDirectory (string directoryName)
		{
			if (verbose && !single_app) {
				Console.WriteLine ("Adding applications from *.webapp files in " +
				"directory '{0}'", directoryName);
			}

			DirectoryInfo di = new DirectoryInfo (directoryName);
			if (!di.Exists) {
				Console.Error.WriteLine ("Directory {0} does not exist.", directoryName);
				return;
			}
			
			foreach (FileInfo fi in di.GetFiles ("*.webapp"))
				AddApplicationsFromConfigFile (fi.FullName);
		}

		public void AddApplicationsFromConfigFile (string fileName)
		{
			if (verbose && !single_app) {
				Console.WriteLine ("Adding applications from config file '{0}'", fileName);
			}

			try {
				XmlDocument doc = new XmlDocument ();
				doc.Load (fileName);

				foreach (XmlElement el in doc.SelectNodes ("//web-application")) {
					AddApplicationFromElement (el);
				}
			} catch {
				Console.WriteLine ("Error loading '{0}'", fileName);
				throw;
			}
		}

		void AddApplicationFromElement (XmlElement el)
		{
			XmlNode n;

			n = el.SelectSingleNode ("enabled");
			if (n != null && n.InnerText.Trim () == "false")
				return;

			string vpath = el.SelectSingleNode ("vpath").InnerText;
			string path = el.SelectSingleNode ("path").InnerText;

			string vhost = null;
			n = el.SelectSingleNode ("vhost");
#if !MOD_MONO_SERVER
			if (n != null)
				vhost = n.InnerText;
#else
			// TODO: support vhosts in xsp.exe
			string name = el.SelectSingleNode ("name").InnerText;
			if (verbose && !single_app)
				Console.WriteLine ("Ignoring vhost {0} for {1}", n.InnerText, name);
#endif

			int vport = -1;
			n = el.SelectSingleNode ("vport");
#if !MOD_MONO_SERVER
			if (n != null)
				vport = Convert.ToInt32 (n.InnerText);
#else
			// TODO: Listen on different ports
			if (verbose && !single_app)
				Console.WriteLine ("Ignoring vport {0} for {1}", n.InnerText, name);
#endif

			AddApplication (vhost, vport, vpath, path);
		}

		public void AddApplicationsFromCommandLine (string applications)
		{
			if (applications == null)
				throw new ArgumentNullException ("applications");
 
			if (applications == "")
				return;

			if (verbose && !single_app) {
				Console.WriteLine ("Adding applications '{0}'...", applications);
			}

			string[] apps = applications.Split (',');

			foreach (string str in apps) {
				string[] app = str.Split (':');

				if (app.Length < 2 || app.Length > 4)
					throw new ArgumentException ("Should be something like " +
					"[[hostname:]port:]VPath:realpath");

				int vport;
				string vhost;
				string vpath;
				string realpath;
				int pos = 0;

				if (app.Length >= 3) {
					vhost = app [pos++];
				} else {
					vhost = null;
				}

				if (app.Length >= 4) {
					// FIXME: support more than one listen port.
					vport = Convert.ToInt16 (app [pos++]);
				} else {
					vport = -1;
				}

				vpath = app [pos++];
				realpath = app [pos++];

				if (!vpath.EndsWith ("/"))
					vpath += "/";
 
				string fullPath = System.IO.Path.GetFullPath (realpath);
				AddApplication (vhost, vport, vpath, fullPath);
			}
		}

		public void UnloadAll ()
		{
			lock (vpathToHost) {
				foreach (VPathToHost v in vpathToHost) {
					v.UnloadHost ();
				}
			}
		}

		public VPathToHost GetApplicationForPath (string vhost, int port, string path,
		                                          bool defaultToRoot)
		{
			if (single_app)
				return (VPathToHost)vpathToHost [0];

			VPathToHost bestMatch = null;
			int bestMatchLength = 0;

			for (int i = vpathToHost.Count - 1; i >= 0; i--) {
				VPathToHost v = (VPathToHost)vpathToHost [i];
				int matchLength = v.vpath.Length;
				if (matchLength <= bestMatchLength || !v.Match (vhost, port, path))
					continue;

				bestMatchLength = matchLength;
				bestMatch = v;
			}

			if (bestMatch != null) {
				lock (bestMatch) {
					if (bestMatch.AppHost == null)
						bestMatch.CreateHost (this, applicationHostType);
				}
				return bestMatch;
			}
			
			if (defaultToRoot)
				return GetApplicationForPath (vhost, port, "/", false);

			if (verbose)
				Console.WriteLine ("No application defined for: {0}:{1}{2}", vhost, port, path);

			return null;
		}

		public VPathToHost GetSingleApp ()
		{
			if (vpathToHost.Count == 1)
				return (VPathToHost)vpathToHost [0];
			return null;
		}

		public void DestroyHost (IApplicationHost host)
		{
			// Called when the host appdomain is being unloaded
			for (int i = vpathToHost.Count - 1; i >= 0; i--) {
				VPathToHost v = (VPathToHost)vpathToHost [i];
				if (v.TryClearHost (host))
					break;
			}
		}

		public void ReloadHost (IApplicationHost host)
		{
			for (int i = vpathToHost.Count - 1; i >= 0; i--) {
				VPathToHost v = (VPathToHost)vpathToHost [i];
				if (v.AppHost == host) {
					v.CreateHost (this, applicationHostType);
					EventHandler<DomainReloadEventArgs> evt = DomainReloadEvent;
					if (evt != null) {
						evt (this, new DomainReloadEventArgs (){VApp = v});
					}
					break;
				}
			}
		}

		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}
}

