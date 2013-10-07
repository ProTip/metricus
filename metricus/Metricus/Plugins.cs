using System;
using Metricus;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using Graphite;

namespace Metricus.Plugins
{

	public interface IInputPlugin
	{
		List<metric> Work();
	}

	public interface IOutputPlugin
	{
		void Work(metric m);
	}


}

