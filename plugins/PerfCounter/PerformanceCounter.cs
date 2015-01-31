using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
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
            public int dynamic_interval { get; set; }
			public List<String> counters { get; set; }
			public List<String> instances { get; set; }
            public string instance_regex { get; set; }
		}

		private class Category {
			public String name { get; set; }
			public Dictionary<Tuple<String, String>, PerformanceCounter> counters { get; set; }
			public List<String> counterNames { get; set; }
			public bool dynamic { get; set; }
            public int dynamicInterval { get; set; }
            public Regex instanceRegex { get; set; }
            private System.Timers.Timer UpdateTimer;
			public Category(string name) {
				this.name = name;
				counters = new Dictionary<Tuple<string, string>, PerformanceCounter>();
			}

		    public void RegisterCounter(String counterName, String instanceName = "") {
				//Console.WriteLine ("Registering counter {0} : {1} : {2}", this.name, counterName, instanceName);
				var key = Tuple.Create (counterName, instanceName);
                lock (counters)
                {
                    if (!counters.ContainsKey(key))
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(instanceName) || (instanceRegex == null || instanceRegex.IsMatch(instanceName)))
                            {
                                var counter = new PerformanceCounter(this.name, counterName, instanceName);
                                counter.NextValue();
                                this.counters.Add(key, counter);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("{0} {1}", e.GetType(), e.Message);
                        }
                    }
                }
			}

			public void UnRegisterCounter(String counterName, String instanceName = "") {
				var key = Tuple.Create (counterName, instanceName);
                lock(counters)
                {
				    if (counters.ContainsKey (key)) 
					    counters.Remove (key);
				}
			}

            public void RemoveStaleCounters(List<Tuple<String, String>> staleCounterKeys)
            {
                foreach (var staleCounterKey in staleCounterKeys)
                    this.UnRegisterCounter(staleCounterKey.Item1, staleCounterKey.Item2);
            }

			public void LoadInstances() {
				Console.WriteLine ("Loading instances for category {0}", this.name);
				var category = new PerformanceCounterCategory (this.name);
				var instanceNames = category.GetInstanceNames ();
				foreach (var instance in instanceNames)
					foreach( var counterName in this.counterNames)                        
						this.RegisterCounter (counterName, instance);								
			}

            public void EnableRefresh()
            {
                if (dynamicInterval == 0) dynamicInterval = 30000;
                UpdateTimer = UpdateTimer ?? new System.Timers.Timer(dynamicInterval);
                UpdateTimer.Elapsed += (m, e) => { this.LoadInstances(); };
                UpdateTimer.Start();
            }
		}

		public override List<metric> Work()
		{
			var metrics = new List<metric>();
            var time = DateTime.Now;
			foreach( var category in this.categories )
			{
				var staleCounterKeys = new List<Tuple<String,String>> ();
                lock (category.counters)
                {
                    foreach (var counter in category.counters)
                    {
                        var pc = counter.Value;
                        try
                        {
                            var newMetric = new metric(pc.CategoryName, pc.CounterName, pc.InstanceName, pc.NextValue(), time);
                            metrics.Add(newMetric);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("{0} {1}", e.GetType(), e.Message);
                            if (e.Message.Contains("does not exist in the specified Category"))
                            {
                                staleCounterKeys.Add(counter.Key);
                                continue;
                            }
                        }
                    }
                }

				//if (category.dynamic) category.LoadInstances ();
                category.RemoveStaleCounters(staleCounterKeys);
			}
			Console.WriteLine ("Collected {0} metrics", metrics.Count);
			return metrics;
		}

		private void LoadCounters()
		{
			var pcList = new List<PerformanceCounter>();

			foreach (var configCategory in config.categories) {
				var newCategory = new Category (configCategory.name);
                if (!string.IsNullOrEmpty(configCategory.instance_regex))
                    newCategory.instanceRegex = new Regex(configCategory.instance_regex);
				var performanceCategory = new PerformanceCounterCategory (configCategory.name);
				newCategory.dynamic = configCategory.dynamic;
                newCategory.dynamicInterval = configCategory.dynamic_interval;
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
                            foreach (var instanceName in instanceNames)
                                newCategory.RegisterCounter(counter, instanceName);
						}
					}
				}
                this.categories.Add(newCategory);
                if (newCategory.dynamic)
                    newCategory.EnableRefresh();
			}
		}
	}
}

