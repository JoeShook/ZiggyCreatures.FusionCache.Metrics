using System.Reflection;
using EmailRouteService.Services;
using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Plugins;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);
var serviceName = "EmailRouteService";

// OpenTelemetry
var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

var resourceBuilder  = ResourceBuilder
    .CreateDefault()
    .AddService(
        serviceName,
        serviceVersion: assemblyVersion, 
        serviceInstanceId: Environment.MachineName);

// Traces
builder.Services.AddOpenTelemetryTracing(options =>
{
    options
        .SetResourceBuilder(resourceBuilder)
        .SetSampler(new AlwaysOnSampler())
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation();

    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
    });
    // options.AddConsoleExporter();
});

// For options which can be bound from IConfiguration.
builder.Services
    .Configure<AspNetCoreInstrumentationOptions>(
        builder.Configuration.GetSection("AspNetCoreInstrumentation")
        );

// Logging
builder.Logging.ClearProviders();

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(resourceBuilder);
    
    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
    });
    // options.AddConsoleExporter();
});

builder.Services.Configure<OpenTelemetryLoggerOptions>(opt =>
{
    opt.IncludeScopes = true;
    opt.ParseStateValues = true;
    opt.IncludeFormattedMessage = true;
});

// Metrics
var emailCacheName = "email";
var domainCacheName = "domain";

builder.Services.AddOpenTelemetryMetrics(options =>
{
    options.SetResourceBuilder(resourceBuilder)
        .AddMeter(emailCacheName, domainCacheName)
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation();

    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
    });

    
    // options.AddConsoleExporter();
});


// Add services to the container.

var emailCache = new MemoryCache(new MemoryCacheOptions());
var domainCache = new MemoryCache(new MemoryCacheOptions());

//
// Cache called "domain"
//
// Register FusionCacheEventSource as a IFusionCachePlugin.
// Note that a MemoryCache object must be created outside of AddFusionCache extension method so that
// FusionCacheEventSource is holding the same object as FusionCache to enabled cache count reporting.
// See line 193 in FusionCacheEventSource.cs
//
builder.Services.AddSingleton<IMemoryCache>(domainCache);
builder.Services.AddSingleton<IFusionCachePlugin>(new FusionMeter(domainCacheName, domainCache));
builder.Services.AddFusionCache(options =>
    options.DefaultEntryOptions = new FusionCacheEntryOptions
    {
        Duration = TimeSpan.FromSeconds(1),
        JitterMaxDuration = TimeSpan.FromMilliseconds(200)
    }
        .SetFailSafe(true, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1))
        .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1))
    );

//
// Cache called "email"
//
// Can't register IFusionCachePlugin here because it is already registered above.  
// This is a second cache and a second instance of FusionMeter to collect metrics for a 
// different cache called "email".
//

builder.Services.AddSingleton(serviceProvider =>
{
    var logger = serviceProvider.GetService<ILogger<ZiggyCreatures.Caching.Fusion.FusionCache>>();

    var fusionCacheOptions = new FusionCacheOptions
    {
        DefaultEntryOptions = new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromSeconds(1),
                JitterMaxDuration = TimeSpan.FromMilliseconds(200)
            }
            .SetFailSafe(true, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1))
            .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1))
    };

    var metrics = new FusionMeter(emailCacheName, emailCache);
    var fusionCache = new ZiggyCreatures.Caching.Fusion.FusionCache(fusionCacheOptions, emailCache, logger);
    metrics.Start(fusionCache);

    return new DnsServiceCache(fusionCache);
});




// Register typed client https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-6.0#typed-clients
builder.Services.AddHttpClient<DnsService>();
builder.Services.AddHttpClient<DomainService>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
