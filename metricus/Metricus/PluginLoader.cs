using System;
using System.Collections.Generic;
using Metricus.Plugin;
using System.IO;
using System.Reflection;

namespace Metricus
{
	public static class PluginLoader<T>
	{
		public static ICollection<Type> GetPlugins(string path)
		{
			//Console.WriteLine ("Loading plugins from path {0} in directory {1}", path, Directory.GetCurrentDirectory().ToString());
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
			string[] dllFileNames = null;
			if (Directory.Exists (path)) 
			{
				dllFileNames = Directory.GetFiles (path, "*.dll");

				ICollection<Assembly> assemblies = new List<Assembly> (dllFileNames.Length);
				foreach (var dllFile in dllFileNames) {
					AssemblyName an = AssemblyName.GetAssemblyName (dllFile);
					Assembly assembly = Assembly.Load (an);
					//Console.WriteLine ("Adding assembly :" + assembly.FullName);
					assemblies.Add (assembly);
				}

				Type pluginType = typeof(T);
				ICollection<Type> pluginTypes = new List<Type> ();
				foreach (var assembly in assemblies) {
					if (assembly != null) 
					{
						Type[] types = assembly.GetTypes ();
						foreach (Type type in types) {
							if (type.IsInterface || type.IsAbstract) 
							{
								continue;
							} else {
								if (type.GetInterface (pluginType.FullName) != null) {
									//Console.WriteLine ("Type matches interface: " + type.ToString ());
									pluginTypes.Add (type);
								} else {
									//Console.WriteLine("Type does not match interface.");
								}
							}
						}
					}
				}

//				ICollection<IInputPlugin> plugins = new List<IInputPlugin> (pluginTypes.Count);
//				foreach (var type in pluginTypes) 
//				{
//					IInputPlugin plugin = (IInputPlugin)Activator.CreateInstance (type);
//					plugins.Add (plugin);
//				}
				return pluginTypes;
			}
			return null;
		}
	}
}

