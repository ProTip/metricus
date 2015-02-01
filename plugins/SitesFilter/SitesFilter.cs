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
        private ServerManager ServerManager;
        private System.Timers.Timer LoadSitesTimer;
        private Object RefreshLock = new Object();

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
            LoadSitesTimer = new System.Timers.Timer(60000);
            LoadSitesTimer.Elapsed += (o, e) => this.LoadSites();
            LoadSitesTimer.Start();
		}

		public override List<metric> Work(List<metric> m) {
            lock(RefreshLock)
            {
                if (config.Categories.ContainsKey(ConfigCategories.AspNetApplications))
                {
                    m = FilterAspNetC.Filter(m, this.siteIDtoName, config.Categories[ConfigCategories.AspNetApplications].PreserveOriginal);
                }
                if (config.Categories.ContainsKey(ConfigCategories.Process))
                    m = FilterWorkerPoolProcesses.Filter(m, ServerManager, config.Categories[ConfigCategories.Process].PreserveOriginal);
            }
			return m;
		}

        public class FilterWorkerPoolProcesses
        {
            public static string IdCategory = ConfigCategories.Process;
            public static string IdCounter = "ID Process";
            public static Dictionary<string, int> WpNamesToIds = new Dictionary<string, int>();

            public static List<metric> Filter(List<metric> metrics, ServerManager serverManager,bool preserveOriginal)
            {
                // "Listen" to the process id counters to map instance names to process id's
                metric m;
                int wpId;
                var originalMetricsCount = metrics.Count;                
                for (int x = 0; x < originalMetricsCount;x++ )
                {
                    m = metrics[x];
                    if (m.category != IdCategory)
                        continue;
                    if (m.type == IdCounter)
                    {
                        WpNamesToIds[m.instance] = (int)m.value;
                        continue;
                    }

                    if (m.instance.StartsWith("w3wp", StringComparison.Ordinal) && WpNamesToIds.TryGetValue(m.instance, out wpId))
                    {
                        for (int y = 0; y < serverManager.WorkerProcesses.Count; y++)
                        {
                            if (serverManager.WorkerProcesses[y].ProcessId == wpId)                            
                            {
                                m.instance = serverManager.WorkerProcesses[y].AppPoolName;
                                switch(preserveOriginal)
                                {
                                    case true:
                                        metrics.Add(m); break;
                                    case false:
                                        metrics[x] = m; break;
                                }
                            }
                        }
                    }                    
                }
                return metrics;
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
            lock (RefreshLock)
            {
                if(ServerManager != null)
                    ServerManager.Dispose();
                ServerManager = new Microsoft.Web.Administration.ServerManager();
                siteIDtoName.Clear();
                foreach (var site in ServerManager.Sites)
                {
                    this.siteIDtoName.Add((int)site.Id, site.Name);
                }
                this.siteIDtoName.PrintDump();
            }
		} 
	}
}