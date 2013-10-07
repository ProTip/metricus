using System;
using Metricus;
using Metricus.Plugins;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using Graphite;

namespace Metricus.Pluginz
{

	public class AspNetInputPlugin : InputPlugin, IInputPlugin
	{
		private List<PerformanceCounter> performanceCounters = new List<PerformanceCounter> ();

		public AspNetInputPlugin(PluginManager pm) : base(pm) { this.LoadCounters(); }

		public override List<metric> Work()
		{
			var metrics = new List<metric>();
			foreach( var pc in performanceCounters)
			{
				try {
					metrics.Add (new metric( pc.CategoryName, pc.CounterName, pc.InstanceName, pc.NextValue(), DateTime.Now));
				} catch(Exception e) {
					Console.WriteLine (e.Message);
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
			var category = new PerformanceCounterCategory ("ASP.NET Applications");
			foreach (var instance in category.GetInstanceNames())
			{
				this.RegisterPerformanceCounters (new PerformanceCounter ("ASP.NET Applications", "Request Execution Time", instance));
			}
		}

	}

	public class BasicInputPlugin : InputPlugin, IInputPlugin
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
				try {
					metrics.Add (new metric( pc.CategoryName, pc.CounterName, pc.InstanceName, pc.NextValue(), DateTime.Now));
				} catch(Exception e) {
					Console.WriteLine (e.Message);
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

	public class BasicOutputPlugin : OutputPlugin, IOutputPlugin
	{
		public BasicOutputPlugin(PluginManager pm) : base(pm) {}

		public override void Work(metric theMetric)
		{
			Console.WriteLine ("{1}.{2}.{3} : {0}", theMetric.value,theMetric.category,theMetric.instance,theMetric.type);
		}
	}

	public class GraphiteOutputPlugin : OutputPlugin, IOutputPlugin
	{
		private PluginManager pm;
		private string graphiteHostname;
		private int graphitePort;

		public GraphiteOutputPlugin(PluginManager pm) : base(pm) 
		{ 
			this.pm = pm;
			graphiteHostname = "10.71.20.78";
			graphitePort = 2003; 
		}

		public override void Work(metric theMetric)
		{
			using (var client = new GraphiteUdpClient (graphiteHostname, graphitePort, pm.Hostname )) {
				var path = theMetric.category;
				if ( theMetric.instance != "" ) path += "." + theMetric.instance;
				path += "." + theMetric.type;
				path = path.ToLower ();
				client.Send(path, (int)theMetric.value);
			}

		}
	}
}

