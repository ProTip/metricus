using System;
using Metricus.Plugin;

namespace Metricus.Plugins
{
	public class ConsoleOut : OutputPlugin, IOutputPlugin
	{
		public ConsoleOut(PluginManager pm) : base(pm) {}

		public override void Work(metric theMetric)
		{
			Console.WriteLine ("{1}.{2}.{3} : {0}", theMetric.value,theMetric.category,theMetric.instance,theMetric.type);
		}
	}
}