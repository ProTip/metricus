using System;
using System.Collections.Generic;
using Metricus.Plugin;
using System.IO;
using System.Reflection;
using System.Diagnostics;
namespace Metricus
{
	public static class PluginLoader<T>
	{
		public static ICollection<Type> GetPlugins(string path)
		{
			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
			string[] dllFileNames = null;
			if (Directory.Exists (path)) 
			{
				dllFileNames = Directory.GetFiles (path, "*.dll");

				ICollection<Assembly> assemblies = new List<Assembly> (dllFileNames.Length);
				foreach (var dllFile in dllFileNames) {
					AssemblyName an = AssemblyName.GetAssemblyName (dllFile);
					Assembly assembly = Assembly.Load (an);
					Debug.WriteLine ("Adding assembly :" + assembly.FullName);
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
									Debug.WriteLine ("Type matches interface: " + type.ToString ());
									pluginTypes.Add (type);
								} else {
									Debug.WriteLine("Type does not match interface.");
								}
							}
						}
					}
				}
				return pluginTypes;
			}
			return null;
		}
	}
}

