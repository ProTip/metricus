using System;
using Metricus;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using Graphite;

namespace Metricus.Plugin
{

	public interface IInputPlugin
	{
		List<metric> Work();
	}

	public interface IOutputPlugin
	{
		void Work(metric m);
	}

	public interface IFilterPlugin
	{
		List<metric> Work(List<metric> m);
	}
}