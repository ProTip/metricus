using System;
using Newtonsoft.Json;
using Metricus.Plugin;

namespace Metricus.Plugins
{
	public class ConsoleOut : OutputPlugin, IOutputPlugin
	{
		public ConsoleOut(PluginManager pm) : base(pm) {}

		public override void Work(metric theMetric)
		{
			Console.WriteLine (JsonConvert.SerializeObject (theMetric, Formatting.Indented));
		}
	}
}