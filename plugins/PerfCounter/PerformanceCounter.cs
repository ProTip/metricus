using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using ServiceStack.Text;
using Metricus.Plugin;

namespace Metricus.Plugins
{
	public class PerfCounter : InputPlugin, IInputPlugin
	{
		private List<Category> categories = new List<Category>();
		private PerfCounterConfig config;

		public PerfCounter(PluginManager pm) : base(pm)
		{
			var path = Path.GetDirectoryName (Assembly.GetExecutingAssembly().Location);
			var configFile = path + "/config.json";
			Console.WriteLine ("Loading config from {0}", configFile);
			config = JsonSerializer.DeserializeFromString<PerfCounterConfig> (File.ReadAllText (path + "/config.json"));
			Console.WriteLine ("Loaded config : {0}", config.Dump ());
			this.LoadCounters ();
		}

		private class PerfCounterConfig {
			public List<ConfigCategory> categories { get; set; }
		}

		private class ConfigCategory {
			public String name { get; set; }
			public bool dynamic { get; set; }
			public List<String> counters { get; set; }
			public List<String> instances { get; set; }
		}

		private class Category {
			public String name { get; set; }
			public Dictionary<Tuple<String, String>, PerformanceCounter> counters { get; set; }
			public List<String> counterNames { get; set; }
			public bool dynamic { get; set; }

			public Category(string name) {
				this.name = name;
				counters = new Dictionary<Tuple<string, string>, PerformanceCounter>();
			}

		    public void RegisterCounter(String counterName, String instanceName = "") {
				Console.WriteLine ("Registering counter {0} : {1} : {2}", this.name, counterName, instanceName);
				var key = Tuple.Create (counterName, instanceName);
				if( ! counters.ContainsKey(key) ) {
					var counter = new PerformanceCounter (this.name, counterName, instanceName);
					counter.NextValue ();
					this.counters.Add (key, counter);
				}
			}

			public void UnRegisterCounter(String counterName, String instanceName = "") {
				var key = Tuple.Create (counterName, instanceName);
				if (counters.ContainsKey (key)) {
					counters.Remove (key);
				}
			}

			public void LoadInstances() {
				Console.WriteLine ("Loading instances for category {0}", this.name);
				var category = new PerformanceCounterCategory (this.name);
				var instanceNames = category.GetInstanceNames ();
				foreach (var instance in instanceNames) {
					foreach( var counterName in this.counterNames){
						this.RegisterCounter (counterName, instance);
					}
				}
			}
		}

		public override List<metric> Work()
		{
			var metrics = new List<metric>();

			foreach( var category in this.categories )
			{
				var staleCounterKeys = new List<Tuple<String,String>> ();
				foreach (var counter in category.counters) {
					var pc = counter.Value;
					try {
						var newMetric = new metric (pc.CategoryName, pc.CounterName, pc.InstanceName, pc.NextValue (), DateTime.Now);
						metrics.Add (newMetric);
					} catch (Exception e) {
						Console.WriteLine ("{0} {1}", e.GetType (), e.Message);
						if (e.Message.Contains ("does not exist in the specified Category")) {
							staleCounterKeys.Add (counter.Key);
							continue;
						}
					}
				}

				if (category.dynamic) category.LoadInstances ();						
				foreach (var staleCounterKey in staleCounterKeys) {
					category.UnRegisterCounter (staleCounterKey.Item1, staleCounterKey.Item2);
				}
			}
			return metrics;
		}

		private void LoadCounters()
		{
			var pcList = new List<PerformanceCounter>();

			foreach (var configCategory in config.categories) {
				var newCategory = new Category (configCategory.name);
				var performanceCategory = new PerformanceCounterCategory (configCategory.name);
				newCategory.dynamic = configCategory.dynamic;
				newCategory.counterNames = configCategory.counters;
				foreach (var counter in configCategory.counters) {
					if (configCategory.instances != null) {
						foreach (var configInstance in configCategory.instances) {
							Console.WriteLine ("Registering instance {0}", configInstance);
							newCategory.RegisterCounter (counter, configInstance);
						}
					} else {
						var instanceNames = performanceCategory.GetInstanceNames ();
						if (instanceNames.Length == 0) {
							newCategory.RegisterCounter (counter);
						} else {
							foreach (var instance in instanceNames) {
								newCategory.RegisterCounter (counter, instance);
							}
						}
					}
				}
				this.categories.Add (newCategory);	
			}
		}
	}
}

