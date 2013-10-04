using System;
using System.Collections.Generic;

namespace Metricus
{
	public abstract class Plugin
	{
		private PluginManager pm;

		public Plugin (PluginManager pm)
		{
			this.pm = pm;
			pm.RegisterPlugin (this);
		}
	
	}

	public abstract class InputPlugin : Plugin
	{
		public InputPlugin(PluginManager pm) : base(pm)
		{
		}

		public abstract List<metric> Work ();
	}


	public abstract class OutputPlugin : Plugin
	{
		public OutputPlugin(PluginManager pm) : base(pm)
		{
		}

		public abstract void Work (metric outputString);
	}

	public abstract class FilterPlugin : Plugin
	{
		public FilterPlugin(PluginManager pm) : base(pm)
		{
		}
	}

	public class PluginManager
	{
		List<InputPlugin> inputPlugins = new List<InputPlugin>();
		List<OutputPlugin> outputPlugins = new List<OutputPlugin>(); 
		List<FilterPlugin> filterPlugins= new List<FilterPlugin>();

		public PluginManager() {}

		public void RegisterInputPlugin( InputPlugin plugin) { inputPlugins.Add (plugin); }

		public void RegisterOutputPlugin( OutputPlugin plugin ) { outputPlugins.Add (plugin); }

		public void RegisterFilterPlugin( FilterPlugin plugin) { filterPlugins.Add (plugin); }

		public void RegisterPlugin( Plugin plugin)
		{
			Console.WriteLine ("Registering plugin of type: " + plugin.GetType().BaseType);
			switch ((plugin.GetType ().BaseType.ToString())) 
			{
			case "Metricus.InputPlugin":
				Console.WriteLine ("Registering InputPlugin");
				this.RegisterInputPlugin ((InputPlugin)plugin);
				break;
			case "Metricus.OutputPlugin":
				Console.WriteLine ("Registering InputPlugin");
				this.RegisterOutputPlugin ((OutputPlugin)plugin);
				break;
			case "Metricus.FilterPlugin":
				Console.WriteLine ("Registering FilterPlugin");
				this.RegisterFilterPlugin ((FilterPlugin)plugin);
				break;
			default:
				throw new Exception ("Invalid plugin type.");
			}
		}

		public void ListInputPlugins()
		{
			foreach (Plugin plugin in inputPlugins) 
			{
				Console.WriteLine (plugin.GetType ());
			}
		}

		public void Tick()
		{
			foreach (InputPlugin iPlugin in inputPlugins) 
			{
				var results = iPlugin.Work ();
				foreach ( OutputPlugin oPlugin in outputPlugins)
				{
					foreach(var result in results) { oPlugin.Work (result); }
				}
			}
		}

	}

	public struct metric {
		public float value;
		public DateTime timestamp;
		public String name;
		public metric(float val, DateTime time, string theName)
		{
			value = val;
			name = theName;
			timestamp = time;
		}

	}

}