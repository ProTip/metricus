using System;
using Metricus;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace Metricus.Plugin
{

	public interface IInputPlugin
	{
		List<metric> Work();
	}

	public interface IOutputPlugin
	{
		void Work(List<metric> m);
	}

	public interface IFilterPlugin
	{
		List<metric> Work(List<metric> m);
	}
}