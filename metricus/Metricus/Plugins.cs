using System;
using Metricus;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace Plugins
{

	public class AspNetInputPlugin : InputPlugin
	{
		private List<PerformanceCounter> performanceCounters = new List<PerformanceCounter> ();

		public AspNetInputPlugin(PluginManager pm) : base(pm)
		{
			this.LoadCounters();
		}

		public override List<metric> Work()
		{
			var metrics = new List<metric>();
			foreach( var pc in performanceCounters)
			{
				metrics.Add (new metric( pc.CategoryName, pc.CounterName, pc.InstanceName, pc.NextValue(), DateTime.Now));
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
			var category = new PerformanceCounterCategory ("ASP.NET Applications");
			foreach (var instance in category.GetInstanceNames())
			{
				this.RegisterPerformanceCounters (new PerformanceCounter ("ASP.NET Applications", "Request Execution Time", instance));
			}
		}

	}

	public class BasicInputPlugin : InputPlugin
	{
		private List<PerformanceCounter> performanceCounters = new List<PerformanceCounter>();

		public BasicInputPlugin(PluginManager pm) : base(pm)
		{
			this.LoadCounters ();
		}

		public override List<metric> Work()
		{
		    var metrics = new List<metric>();
			foreach( var pc in performanceCounters)
			{
				metrics.Add (new metric( pc.CategoryName, pc.CounterName, pc.InstanceName, pc.NextValue(), DateTime.Now)); 
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

	public class BasicOutputPlugin : OutputPlugin
	{
		public BasicOutputPlugin(PluginManager pm) : base(pm)
		{

		}

		public override void Work(metric theMetric)
		{
			Console.WriteLine ("{1}.{2}.{3} : {0}", theMetric.value,theMetric.category,theMetric.instance,theMetric.type);
		}
	}
}

