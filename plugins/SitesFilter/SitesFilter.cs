using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using ServiceStack.Text;
using Microsoft.Web.Administration;
using System.Text.RegularExpressions;
using System.Linq;

namespace Metricus.Plugin
{
	public class SitesFilter : FilterPlugin, IFilterPlugin
	{
		private SitesFilterConfig config;
		private Dictionary<int, string> siteIDtoName;

		private class SitesFilterConfig {
			public Dictionary<string,ConfigCategory> Categories { get; set; }
		}

		private class ConfigCategory {
			public bool PreserveOriginal { get; set; }
		}

		public SitesFilter(PluginManager pm) : base(pm)	{
			var path = Path.GetDirectoryName (Assembly.GetExecutingAssembly().Location);
			var configFile = path + "/config.json";
			Console.WriteLine ("Loading config from {0}", configFile);
			config = JsonSerializer.DeserializeFromString<SitesFilterConfig> (File.ReadAllText (path + "/config.json"));
			Console.WriteLine ("Loaded config : {0}", config.Dump ());
			siteIDtoName = new Dictionary<int, string> ();
			this.LoadSites ();
		}

		public override List<metric> Work(List<metric> m) {
			this.LoadSites ();
			if ( config.Categories.ContainsKey("ASP.NET Applications")) {
				m = FilterAspNet (m);
			}
			return m;
		}

		public List<metric> FilterAspNet(List<metric> m) {
			Regex RegexAspNetApplications = new Regex ("_LM_W3SVC");
			var returnMetrics = new List<metric> ();
			foreach (var metric in m) {
				var newMetric = metric;
				if (metric.instance.Contains ("_LM_W3SVC")) {
					var matchID = new Regex ("_LM_W3SVC_(\\d+)_");
					var match = matchID.Match (metric.instance);
					var id = match.Groups [1].Value;
					string siteName;
					if (this.siteIDtoName.TryGetValue (int.Parse (id), out siteName)) {
						newMetric.instance = Regex.Replace (metric.instance, "_LM_W3SVC_(\\d+)_ROOT_?", siteName + "/");
						returnMetrics.Add (newMetric);
					}
					if (config.Categories ["ASP.NET Applications"].PreserveOriginal)
						returnMetrics.Add (metric);
				} else {
					returnMetrics.Add (newMetric);
				}
			}
			return returnMetrics;
		}
		
		public void LoadSites() {
			siteIDtoName.Clear ();
			var mgr = new Microsoft.Web.Administration.ServerManager ();
			foreach (var site in mgr.Sites) {
				this.siteIDtoName.Add ((int)site.Id, site.Name);
			}
			this.siteIDtoName.PrintDump ();
		} 
	}
}