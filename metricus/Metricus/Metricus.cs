using System;
using System.Collections;

namespace Metricus
{
	public class Plugin
	{
		private PluginManager pluginManager;

		public Plugin (PluginManager pluginManager)
		{
			self.pluginManager = pluginManager;
		}
	
	}

	public class InputPlugin : Plugin
	{
		public InputPlugin()
		{
				
		}
	}


	public class OutputPlugin : Plugin
	{

	}

	public class FilterPlugin : Plugin
	{
		public FilterPlugin() {}
	}

	public class PluginManager
	{
		IList<InputPlugin> inputPlugins;
		IList<OutputPlugin> outputPlugins; 
		IList<FilterPlugin> filterPlugins;

		public PluginManager() {}

		public void RegisterInputPlugin( InputPlugin plugin) { inputPlugins.Add (plugin); }

		public void RegisterOutputPlugin( OutputPlugin plugin ) { filterPlugins.Add (plugin); }

		public void RegisterFilterPlugin()
		{

		}
	}

}