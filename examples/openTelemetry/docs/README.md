![FusionCache logo](../../../artwork/logo-plugin-128x128.png)

# Example usage of FusionCache.OpenTelemetry.Plugin

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)

## The OpenTelemetry example is a collection of services used to demonstrate OpenTelemetry Metrics produced from [FusionCache](https://github.com/jodydonetti/ZiggyCreatures.FusionCache) events

The example is built to produce all events [FusionCache](https://github.com/jodydonetti/ZiggyCreatures.FusionCache) could produce so a user of the plugin can get an understanding of how to use this plugin and what metrics to expect.  This particular example can report to the console, Influx Database or Influx Cloud.  Influx Cloud is very easy to setup and is recomended to get a good visual of the metrics.  See the end of this doc for guidance.  We will also be using [SuperBenchMarker](https://github.com/aliostad/SuperBenchmarker) to produce a load for observing metrics.

While the spirit of this example is to demonstrate OpenTelemetry Metrics, it will also demonstrate Trace and Log telemetry. When you experience Metrics, Traces and Logs together you really start to see how valuable this data is.
## Use Case

The fictisious web services are modeling the resolutions of individual email addresses.  
Think of it like a mocked up SMTP resolution process.  Use the [MultiService sequence diagram](images/MultiServiceExampleSequence.png) for reference. 
There are three web servics in this lab example.  

### EmailRoutService
EmailRoutService is the publicly facing web service that our load test client will request email addresses from.


### DomainService

DomainService resolves metadata about the host part of email address.
A cache called "domain" cache domain data and a FusionCacheEvenSource plugin instance will collect metrics.  
A DataManager will lookup data in the MockDomainCertData.json file representing a database access strategy.  
In the `Datamanager.GetDomain` method there are some artificial random delays and excetptions that will produce all 
of the [FusionCache](https://github.com/jodydonetti/ZiggyCreatures.FusionCache) events.  
Note not all Domains are enabled as one can see from the mock data.  

### DnsService

The second is email.  If a domain was found and it is enabled then a email address can then be found which after 
all is what the load test harness will be testing.  Email is represented as another cache called "email" and another 
instance of FusionCacheEventSource plugin for tracing metrics independent of "domain".  
The `DataManager.GetEmailRoute` method has the same artificial random delays and errors that `DataManager.GetDomain` has.


## OpenTelemetry

Notice in the [MultiService sequence diagram](images/MultiServiceExampleSequence.png), 
the bottom swim is labeled as System.Diagnostics.ActivityListener.  
For now just know that each web service will be enabled for Metrics, Traces and Logs.  
We will also be including an exporter that will export metrics over gRPC to an OpenTelemetry Collector.
Notice also the Docker Compose box in the upper right side.  You will find a DockerCompose.yaml file where you can setup
this same lab enviroment locally.  

### Configuration Convention

This example is intended to be experimented with.  Choose your exporter for Logging, Metrics and Tracing independently.  
I followed the same pattern as the [opentelemetry-dotnet](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/examples/AspNetCore)
ASPNetCore example.  From the appsettings.xml you can control these settings.  




