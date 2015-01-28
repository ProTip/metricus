using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Metricus.Plugin;

namespace Metricus.Plugins
{
	public class ConsoleOut : OutputPlugin, IOutputPlugin
	{
		public ConsoleOut(PluginManager pm) : base(pm) {}

		public override void Work(List<metric> m)
		{
            foreach(var theMetric in m)
			    Console.WriteLine (JsonConvert.SerializeObject (theMetric, Formatting.Indented));
		}
	}
}