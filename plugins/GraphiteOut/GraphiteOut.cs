using System;
using System.Text.RegularExpressions;
using Graphite;
using System.IO;
using System.Reflection;
using Metricus.Plugin;
using ServiceStack.Text;

namespace Metricus.Plugins
{
	public class GraphiteOut : OutputPlugin, IOutputPlugin
	{
		class GraphiteOutConfig
		{
			public String Hostname { get; set; }
			public String Prefix { get; set; }
			public int Port { get; set; }
		}


		private PluginManager pm;
		private string graphiteHostname;
		private int graphitePort;
		private GraphiteOutConfig config;

		public GraphiteOut(PluginManager pm) : base(pm) 
		{ 
			var path = Path.GetDirectoryName (Assembly.GetExecutingAssembly().Location);
			config = JsonSerializer.DeserializeFromString<GraphiteOutConfig> (File.ReadAllText (path + "/config.json"));
			Console.WriteLine ("Loaded config : {0}", config.Dump ());
			this.pm = pm;
			graphiteHostname = config.Hostname;
			graphitePort = config.Port; 
		}

		public override void Work(metric theMetric)
		{
			//TODO: This client sends one packet per metric?!
			this.FormatMetric (ref theMetric);
			using (var client = new GraphiteUdpClient (graphiteHostname, graphitePort, config.Prefix + "." + pm.Hostname )) {
				var path = theMetric.category;
				path += (theMetric.instance != "") ? "." + theMetric.instance : ".total";
				path += "." + theMetric.type;
				path = path.ToLower ();
				client.Send(path, (int)theMetric.value);
			}

		}

		private void FormatMetric(ref metric m)
		{
			m.category = Regex.Replace(m.category,"(\\s+|\\.|/)","_");
			m.type = Regex.Replace(m.type,"(\\s+|\\.|/)","_");
			m.instance = Regex.Replace(m.instance,"(\\s+|\\.|/)","_");
		}
	}
}

