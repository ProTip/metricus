using System;
using System.Timers;
using Metricus;
using Metricus.Plugins;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using Topshelf;
using ServiceStack.Text;

namespace metricus
{
	class MetricusConfig
	{
		public string Host { get; set; }
		public int Interval { get; set; }
	}

	class MetricusService : ServiceControl
	{
		readonly System.Timers.Timer _timer;

		private PluginManager pluginManager;

		public MetricusService() 
		{
			var config = JsonSerializer.DeserializeFromString<MetricusConfig> (File.ReadAllText ("config.json"));
			Console.WriteLine("Config loaded: {0}", config.Dump() );

			_timer = new System.Timers.Timer (config.Interval);
			_timer.Elapsed += new ElapsedEventHandler (Tick);
			pluginManager = new PluginManager (config.Host);
			//Console.WriteLine ("Hello World!");
		}

		public bool Start(HostControl hostControl)
		{
			this.LoadPlugins ();
			_timer.Start ();
			return true;
		}

		public bool Stop(HostControl hostControl)
		{
			_timer.Stop ();
			return true;
		}

		private void LoadPlugins()
		{
			string[] dllFileNames = null;

			Console.WriteLine (Directory.GetCurrentDirectory().ToString());
			if (Directory.Exists ("Plugins")) {
				//Console.WriteLine ("Loading plugins");
				dllFileNames = Directory.GetFiles ("Plugins", "*.dll");
			} else {
				//Console.WriteLine ("Plugin directory not found!");
			}

			foreach (var plugin in dllFileNames) {
				//Console.WriteLine (plugin);
			}



			var inputPlugins = PluginLoader<IInputPlugin>.LoadPlugins ("Plugins");
			foreach (Type type in inputPlugins) {
				Activator.CreateInstance(type, pluginManager);
			}

			var outputPlugins = PluginLoader<IOutputPlugin>.LoadPlugins ("Plugins");
			foreach (Type type in outputPlugins) {
				Activator.CreateInstance(type, pluginManager);
			}
		}

		private void Tick (object source, ElapsedEventArgs e)
		{
			var start = DateTime.Now;
			this.pluginManager.Tick ();
			var elapsed = DateTime.Now - start;
		}
	}

	public class Program
	{
		public static void Main (string[] args)
		{
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
			HostFactory.Run (x =>
			{
				x.Service<MetricusService>();
				x.RunAsLocalSystem();			
				x.SetServiceName("Metricus");
				x.SetDescription("Metric collection and ouput service");
			});
		}
	}
}
