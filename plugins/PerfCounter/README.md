PerformanceCounter
=============
This plugin was the primary reason for creating the project.  It collects performance counters according to the JSON
configuration file.


###config.json
This plugin's configuration is centered around "categories", and it supports a few different configuration scenarios.  What follows is an abridged configuration that represents these scenarios as well as a breakdown:

```json
{
  "categories" : [
    {
      "name" : "Processor",
      "counters" : [ "% Processor Time" ],
      "instances" : [ "_total" ]
    },
    {
      "name" : "Memory",
      "counters" : [ "Pages/sec" ]
    },
    {
      "name" : "ASP.NET Applications",
      "counters" : [ "Requests/Sec", "Request Execution Time" ]
    },
    {
      "name" : "PhysicalDisk",
      "dynamic" : True,
      "counters" : [ "Split IO/Sec" ]
    },
  [
}

```

####Category->Counters->Instances
```json
"categories" : [
    {
      "name" : "Processor",
      "counters" : [ "% Processor Time" ],
      "instances" : [ "_total" ]
    },
```
You know the counters and the instances you want to collect.  Everything is explicitly configured.

####Category->Counters
```json
    {
      "name" : "Memory",
      "counters" : [ "Pages/sec" ]
    },
    {
      "name" : "ASP.NET Applications",
      "counters" : [ "Requests/Sec", "Request Execution Time" ]
    },
```
You know the counters you want but they either do not have instances, the prior, or you want all the instances, the latter.  This is a two-for.

####Dynamic
```json
{
      "name" : "PhysicalDisk",
      "dynamic" : True,
      "counters" : [ "Split IO/Sec" ]
    },
```
Some categoies have dynamic instances.  In the case of disks if a new disk is added, or a disk is removed, the instances available will change.  Setting dynamic to true currently loads the instance list every interval and collects all counters on all of them.
