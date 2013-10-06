using System;
using Metricus;
using Plugins;
using System.Threading;
using System.Diagnostics;

namespace metricus
{
	class MainClass
	{
		public static void Main (string[] args)
		{

			Console.WriteLine ("Hello World!");
			PluginManager pluginManager = new PluginManager ();
			new BasicInputPlugin (pluginManager);
			new BasicOutputPlugin (pluginManager);
			new AspNetInputPlugin (pluginManager);
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
