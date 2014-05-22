using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;

namespace Mono.WebServer.HyperFastCgi.Config
{
	public class ConfigUtils
	{
		public static List<WebAppConfig> GetApplicationsFromCommandLine (string applications)
		{
			List<WebAppConfig> applist = new List<WebAppConfig> ();

			if (applications == null)
				throw new ArgumentNullException ("applications");

			if (applications == "")
				return applist;

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
				applist.Add (new WebAppConfig (){VHost = vhost, VPort = vport, VPath = vpath, RealPath = fullPath }); 
			}

			return applist;
		}

		public static List<WebAppConfig> GetApplicationsFromConfigDirectory (string directoryName)
		{
			List<WebAppConfig> applist = new List<WebAppConfig> ();

			DirectoryInfo di = new DirectoryInfo (directoryName);
			if (!di.Exists) {
				Console.Error.WriteLine ("Directory {0} does not exist.", directoryName);
				return applist;
			}

			foreach (FileInfo fi in di.GetFiles ("*.webapp"))
				applist.AddRange(GetApplicationsFromConfigFile (fi.FullName));

			return applist;
		}


		public static List<WebAppConfig> GetApplicationsFromConfigFile (string fileName)
		{
			List<WebAppConfig> applist = new List<WebAppConfig> ();

			try {
				XmlDocument doc = new XmlDocument ();
				doc.Load (fileName);

				foreach (XmlElement el in doc.SelectNodes ("//web-application")) {
//					applist.Add(GetApplicationFromElement (el));
					applist.Add((WebAppConfig)GetConfigFromElement (typeof(WebAppConfig),el));
				}
			} catch {
				Console.WriteLine ("Error loading '{0}'", fileName);
				throw;
			}

			return applist;
		}

		public static List<ConfigInfo> GetConfigsFromFile (string filename, string xmlnode, Type defaultType)
		{
			List<ConfigInfo> configs = new List<ConfigInfo> ();
			XmlDocument doc = new XmlDocument();

			try {
				doc.Load(filename);
			} catch {
				Console.WriteLine ("Error loading '{0}'", filename);
				throw;
			}

			foreach (XmlElement node in doc.SelectNodes("//"+xmlnode)) {
				ConfigInfo configInfo = null;
				XmlAttribute attrType = node.Attributes ["type"];

				if (attrType != null && !String.IsNullOrEmpty (attrType.Value)) {
					Type t=Type.GetType (attrType.Value);

					object[] attrs=t.GetCustomAttributes (typeof(ConfigAttribute), false);

					if (attrs != null && attrs.Length > 0) {
						configInfo = new ConfigInfo () {
							Type = t,
							Config = GetConfigFromElement (((ConfigAttribute)attrs [0]).Type, node)
						};
					} 
				}

				if (configInfo == null) {
					configInfo = new ConfigInfo () {
						Type = null,
						Config = GetConfigFromElement (defaultType, node)
					};
				}

				configs.Add (configInfo);
			}

			return configs;
		}

		static object GetConfigFromElement(Type configType, XmlElement el)
		{
			XmlSerializer ser = new XmlSerializer (configType);

			using (XmlReader rdr = new XmlNodeReader (el)) {
				return ser.Deserialize (rdr);
			}
		}

	}
}

