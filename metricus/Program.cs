using System;
using Metricus;
using Metricus.Plugins;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace metricus
{
	class MainClass
	{

		public static void Main (string[] args)
		{
			string[] dllFileNames = null;

			if (Directory.Exists ("Plugins")) {
				Console.WriteLine ("Loading plugins");
				dllFileNames = Directory.GetFiles ("Plugins", "*.dll");
			} else {
				Console.WriteLine ("Plugin directory not found!");
			}

			foreach (var plugin in dllFileNames) {
				Console.WriteLine (plugin);
			}


			Console.WriteLine ("Hello World!");
			PluginManager pluginManager = new PluginManager ("laptop.co.nz");

			var inputPlugins = PluginLoader<IInputPlugin>.LoadPlugins ("Plugins");
			foreach (Type type in inputPlugins) {
				Activator.CreateInstance(type, pluginManager);
			}

			var outputPlugins = PluginLoader<IOutputPlugin>.LoadPlugins ("Plugins");
			foreach (Type type in outputPlugins) {
				Activator.CreateInstance(type, pluginManager);
			}

			var start = DateTime.Now;
			for (int i=0; i < 10000; i++)
			{
				pluginManager.Tick ();
				Thread.Sleep (5000);
			}
			var elapsed = DateTime.Now - start;
			Console.WriteLine ("Elapsed time: " + elapsed);
			Console.ReadKey ();
		}
	}
}
