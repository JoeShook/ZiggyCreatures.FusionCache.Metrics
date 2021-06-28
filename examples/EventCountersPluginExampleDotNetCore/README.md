# Example usage of FusionCache.EventCounters.Plugin

<div align="center">

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)

</div>

## EventCountersPluginExampleDotNetCore is an example project using the FusionCache.EventCounters.Plugin to plugin to [FusionCache](https://github.com/jodydonetti/ZiggyCreatures.FusionCache)

The example is built to produce all events [FusionCache](https://github.com/jodydonetti/ZiggyCreatures.FusionCache) could produce so a user of the plugin can get an understanding of how to use this plugin and what metrics to expect.  This particular example can report to the console, Influx Database or Influx Cloud.  Influx Cloud is very easy to setup and is recomended to get a good visual of the metrics.  We will also be using [SuperBenchMarker](https://github.com/aliostad/SuperBenchmarker) to produce a load for observing metrics.

## Use Case

The fictisious web service is resolving email address information.  Think of it like a partial SMTP resolution process.  There are two parts to the resolution.  

### Part one

One is domain which is the host part of email address.  This is checked first.  A cache called "domain" cache domain data and a FusionCacheEvenSource plugin instance will collect metrics.  A DataManager will lookup data in the MockDomainCertData.json file representing a database access strategy.  In the `Datamanager.GetDomain` method there are some artificial random delays and excetptions that will produce all of the [FusionCache](https://github.com/jodydonetti/ZiggyCreatures.FusionCache) events.  Note not all Domains are enabled as one can see from the mock data.  

### Part two

The second is email.  If a domain was found and it is enabled then a email address can then be found wich after all is what the load test harness will be testing.  Email is represented as another cache called "email" and another instance of FusionCacheEventSource plugin for tracing metrics independent of "domain".  The `DataManager.GetEmailRoute` method has the same artificial random delays and errors that `DataManager.GetDomain` has.

## Test the example application

In the [superbenchmarker](./tree/main/examples/superbenchmarker) folder there are example launchers for all the sample projects.  For the examples below we will use the following small batch link:

`sb -u "http://localhost:5000/EmailValidator/EmailRoute/{{{email}}}" -f EmailAddressDataSmallBatch.csv -U -c 8 -N 6000 -P 1`

So start you app and run.  The -c 8 is a concurrency of 8.  I have 16 virtual cores and this was plenty to produce interesting metrics data.  -N 6000 means run for 6000 seconds.  -P is how often SuperBenchMarker samples for it's own report.  

