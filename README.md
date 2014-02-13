Metricus
========
Metricus is a .Net metric collection service inspired by collectd but far less sophisticated.  

###Key features

* JSON configuration
* Input/Filter/Output plugins
* ZeroFactories&trade;
    * Seriously though, it's very easy to read the code.
    * And yes, no factories.
* Ephemeral instance support for performance counters.


###Configuration
Configuration is handled through config.json files.  The ~~daemon~~ service configuration file is in the base directory, and each of the plugins has their own file in their respective directories.

####Service Configuration

```json
{
  "Host" : "laptop_co_nz",
  "Interval" : "10000",
  "ActivePlugins" : [
  	"PerformanceCounter",
  	"Graphite",
  	"ConsoleOut"
  ]
}

```
