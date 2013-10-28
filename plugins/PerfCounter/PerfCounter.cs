using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using Metricus.Plugin;

namespace Metricus.Plugins
{
	public class PerfCounter : InputPlugin, IInputPlugin
	{
		private List<PerformanceCounter> performanceCounters = new List<PerformanceCounter>();

		public PerfCounter(PluginManager pm) : base(pm)
		{
			this.LoadCounters ();
		}

		public override List<metric> Work()
		{
			var metrics = new List<metric>();
			foreach( var pc in performanceCounters)
			{
				try {
					metrics.Add (new metric( pc.CategoryName, pc.CounterName, pc.InstanceName, pc.NextValue(), DateTime.Now));
				} catch(Exception e) {
					//Console.WriteLine (e.Message);
				}
			}
			return metrics;
		}

		private void RegisterPerformanceCounters(PerformanceCounter performanceCoutner)
		{
			performanceCoutner.NextValue ();
			this.performanceCounters.Add (performanceCoutner);
		}

		private void LoadCounters()
		{
			var pcList = new List<PerformanceCounter>();
			pcList.Add (new PerformanceCounter ("Processor", "% Processor Time", "_Total"));		          
			pcList.Add (new PerformanceCounter ("Memory", "Available MBytes"));
			foreach (var pc in pcList) { this.RegisterPerformanceCounters (pc); }
		}
	}
}

