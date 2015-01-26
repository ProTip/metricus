using System;
using System.Timers;
using Metricus;
using Metricus.Plugin;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using Topshelf;
using ServiceStack.Text;

namespace Metricus
{
	class MetricusConfig
	{
		public string Host { get; set; }
		public int Interval { get; set; }
		public List<String> ActivePlugins { get; set; }
	}

	class MetricusService : ServiceControl
	{
		readonly System.Timers.Timer _timer;

		private MetricusConfig config;
		private PluginManager pluginManager;
        private Object workLocker = new Object();

		public MetricusService() 
		{
			config = JsonSerializer.DeserializeFromString<MetricusConfig> (File.ReadAllText ("config.json"));
			Console.WriteLine("Config loaded: {0}", config.Dump() );

			_timer = new System.Timers.Timer (config.Interval);
			_timer.Elapsed += new ElapsedEventHandler (Tick);
			pluginManager = new PluginManager (config.Host);
		}

		public bool Start(HostControl hostControl)
		{
			this.LoadPlugins ();
			_timer.Start ();
			return true;
		}

		public bool StartRaw()
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

			if (Directory.Exists ("Plugins")) {
				Console.WriteLine ("Loading plugins");
				foreach (var dir in Directory.GetDirectories("Plugins")) {

					dllFileNames = Directory.GetFiles ("Plugins", "*.dll");
				}
			} else {
				Console.WriteLine ("Plugin directory not found!");
			}

			foreach (var plugin in dllFileNames) {
				Console.WriteLine (plugin);
			}


			foreach (var dir in Directory.GetDirectories("Plugins")) 
			{
				var inputPlugins = PluginLoader<IInputPlugin>.GetPlugins (dir);
				Console.WriteLine (dir.ToString());
				foreach (Type type in inputPlugins) {
					Console.WriteLine (type.Assembly.GetName ().Name);
					if (config.ActivePlugins.Contains (type.Assembly.GetName ().Name)) {
						Console.WriteLine ("Loading plugin {0}", type.Assembly.GetName ().Name);
						Activator.CreateInstance (type, pluginManager);
					}
				}

				var outputPlugins = PluginLoader<IOutputPlugin>.GetPlugins (dir);
				foreach (Type type in outputPlugins) {
					if (config.ActivePlugins.Contains (type.Assembly.GetName ().Name)) {
						Console.WriteLine ("Loading plugin {0}", type.Assembly.GetName ().Name);
						Activator.CreateInstance (type, pluginManager);
					}
				}

				var filterPlugins = PluginLoader<IFilterPlugin>.GetPlugins (dir);
				foreach (Type type in filterPlugins) {
					if (config.ActivePlugins.Contains (type.Assembly.GetName ().Name)) {
						Console.WriteLine ("Loading plugin {0}", type.Assembly.GetName ().Name);
						Activator.CreateInstance (type, pluginManager);
					}
				}
			}
		}

		private void Tick (object source, ElapsedEventArgs e)
		{
			Console.WriteLine ("Tick");
            if (Monitor.TryEnter(workLocker))
            {
                try
                {
                    var start = DateTime.Now;
                    this.pluginManager.Tick();
                    var elapsed = DateTime.Now - start;
                }
                finally
                {
                    Monitor.Exit(workLocker);
                }
            }
            else
                return;
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
				//x.UseNLog();
			});
		}
	}
}
