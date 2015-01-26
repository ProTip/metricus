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
        private FilterWorkerPoolProcesses WorkerPoolFilter = new FilterWorkerPoolProcesses();

		private class SitesFilterConfig {
			public Dictionary<string,ConfigCategory> Categories { get; set; }
		}

		private class ConfigCategory {
			public bool PreserveOriginal { get; set; }
		}

        public class ConfigCategories
        {
            public const string AspNetApplications = "ASP.NET Applications";
            public const string Process = "Process";
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
            if (config.Categories.ContainsKey(ConfigCategories.AspNetApplications))
                m = FilterAspNetC.Filter(m, this.siteIDtoName, config.Categories[ConfigCategories.AspNetApplications].PreserveOriginal);
            if ( config.Categories.ContainsKey(ConfigCategories.Process))
                m = WorkerPoolFilter.Filter(m, config.Categories[ConfigCategories.Process].PreserveOriginal);
			return m;
		}

        public class FilterWorkerPoolProcesses
        {
            public static string IdCategory = ConfigCategories.Process;
            public static string IdCounter = "ID Process";
            public static Regex MatchW3WP = new Regex("^w3wp");
            public Dictionary<string, int> WpNamesToIds = new Dictionary<string, int>();

            public List<metric> Filter(List<metric> metrics, bool preserveOriginal)
            {
                var ServerManager = new ServerManager();
                var returnMetrics = new List<metric>();
                // "Listen" to the process id counters to map instance names to process id's
                foreach (var m in metrics)
                {
                    if (m.category == IdCategory && m.type == IdCounter)
                    {
                        WpNamesToIds[m.instance] = (int)m.value;
                        continue;
                    }

                    if (MatchW3WP.IsMatch(m.instance) && WpNamesToIds.ContainsKey(m.instance))
                    {
                        var newMetric = m;
                        var workerPool = ServerManager.WorkerProcesses.SingleOrDefault(wp => wp.ProcessId == WpNamesToIds[m.instance]);
                        if (workerPool != null)
                            newMetric.instance = workerPool.AppPoolName;
                        returnMetrics.Add(newMetric);
                        if (preserveOriginal)
                            returnMetrics.Add(m);
                    }
                    else
                        returnMetrics.Add(m);                        
                }
                return returnMetrics;
            }
        }

        public class FilterAspNetC
        {
            private static string PathSansId = "_LM_W3SVC";
            private static Regex MatchPathWithId = new Regex("_LM_W3SVC_(\\d+)_");
            private static Regex MatchRoot = new Regex("ROOT_?");

            public static List<metric> Filter(List<metric> metrics, Dictionary<int,string> siteIdsToNames, bool preserveOriginal)
            {
                var returnMetrics = new List<metric>();
                foreach (var metric in metrics)
                {
                    var newMetric = metric;
                    if (metric.instance.Contains(PathSansId))
                    {
                        var match = MatchPathWithId.Match(metric.instance);
                        var id = match.Groups[1].Value;
                        string siteName;
                        if (siteIdsToNames.TryGetValue(int.Parse(id), out siteName))
                        {
                            newMetric.instance = Regex.Replace(metric.instance, "_LM_W3SVC_(\\d+)_ROOT_?", siteName + "/");
                            returnMetrics.Add(newMetric);
                        }
                        if (preserveOriginal)
                            returnMetrics.Add(metric);
                    }
                    else
                        returnMetrics.Add(newMetric);                    
                }
                return returnMetrics;
            }
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