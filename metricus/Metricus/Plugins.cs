using System;
using Metricus;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace Plugins
{
	public class BasicInputPlugin : InputPlugin
	{
		private List<PerformanceCounter> performanceCounters = new List<PerformanceCounter>();

		public BasicInputPlugin(PluginManager pm) : base(pm)
		{
			this.LoadPlugins ();
		}

		public override List<metric> Work()
		{
		    var metrics = new List<metric>();
			foreach( var pc in performanceCounters)
			{
				metrics.Add (new metric (pc.NextValue (), DateTime.Now, pc.CounterName));
			}
			return metrics;
		}

		private void RegisterPerformanceCounters(PerformanceCounter performanceCoutner)
		{
			performanceCoutner.NextValue ();
			this.performanceCounters.Add (performanceCoutner);
		}

		private void LoadPlugins()
		{
			var pcList = new List<PerformanceCounter>();
			pcList.Add (new PerformanceCounter ("Processor", "% Processor Time", "_Total"));		          
			pcList.Add (new PerformanceCounter ("Memory", "Available MBytes"));
			foreach (var pc in pcList) { this.RegisterPerformanceCounters (pc); }
		}
	}

	public class BasicOutputPlugin : OutputPlugin
	{
		public BasicOutputPlugin(PluginManager pm) : base(pm)
		{

		}

		public override void Work(metric theMetric)
		{
			Console.WriteLine (theMetric.value + " " + theMetric.name);
		}
	}
}

