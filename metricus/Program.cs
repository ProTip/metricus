using System;
using System.Timers;
using Metricus;
using Metricus.Plugins;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Topshelf;

namespace metricus
{
	class MetricusService : ServiceControl
	{
		readonly System.Timers.Timer _timer;


		private PluginManager pluginManager;

		public MetricusService() 
		{
			_timer = new System.Timers.Timer (10000);
			_timer.Elapsed += new ElapsedEventHandler (Tick);
			pluginManager = new PluginManager ("laptop.co.nz");
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
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
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
