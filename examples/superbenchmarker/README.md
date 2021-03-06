# Benchmark Testing Examples

Using [SuperBenchmarker](https://github.com/aliostad/SuperBenchmarker) to place a load on example web services.  This allows for experimenting with cache settings and producing metrics.  


## Example used to test EventCountersPluginExampleDotnetCore

sb -u "http://localhost:5000/EmailValidator/EmailRoute/{{{email}}}"  -f EmailAddressData.csv -U -c 10 -N 600 -P 1

Small batch to get metrics results quicker.

sb -u "http://localhost:5000/EmailValidator/EmailRoute/{{{email}}}"  -f EmailAddressDataSmallBatch.csv -U -c 8 -N 600 -P 1



## Example used to test AppMetricsPluginExampleFrameworkOnAspNetCore
sb -u "http://localhost:5000/api/EmailValidator/EmailRoute/{{{email}}}"  -f EmailAddressDataSmallBatch.csv -U -c 8 -N 600 -P 1



## Example used to test AppMetricsPluginExampleFramework
sb -u "http://localhost:52236/api/EmailValidator?emailAddress={{{email}}}"  -f EmailAddressDataSmallBatch.csv -U -c 8 -N 600 -P 1

