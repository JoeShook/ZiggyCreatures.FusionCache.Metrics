# Benchmark Testing Examples

Using [SuperBenchmarker](https://github.com/aliostad/SuperBenchmarker) to place a load on example web services.  This allows for experimenting with cache settings and producing metrics.  

 ## Example used to test EventCountersPluginExampleDotnetCore

sb -u "http://localhost:5000/EmailValidator/EmailRoute/{{{email}}}"  -f EmailAddressData.csv -U -c 10 -N 600 -P 1


## Example used to test EventCountersPluginExampleDotnetCore
sb -u "http://localhost:37276/api/EmailValidator/EmailRoute/{{{email}}}"  -f EmailAddressData.csv -U -c 10 -N 600 -P 1