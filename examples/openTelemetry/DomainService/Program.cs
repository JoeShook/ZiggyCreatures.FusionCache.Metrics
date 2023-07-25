using System.Diagnostics;
using System.Reflection;
using DomainService.Services;
using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Services;
using Services.Model;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Plugins;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.OpenTelemetry;
using DataFileMonitorService = DomainService.Services.DataFileMonitorService;

var builder = WebApplication.CreateBuilder(args);
var serviceName = "OtelDomainService";

// OpenTelemetry
var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService(
        serviceName,
        serviceVersion: assemblyVersion,
        serviceInstanceId: Environment.MachineName);

// Traces
builder.Services.AddOpenTelemetry()
    .WithTracing(traceBuilder =>
    {
    traceBuilder
        .SetResourceBuilder(resourceBuilder)
        .SetSampler(new AlwaysOnSampler())
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation();

    traceBuilder.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
    });

#if DEBUG
    // options.AddConsoleExporter();
#endif
});

// For options which can be bound from IConfiguration.
builder.Services
    .Configure<AspNetCoreInstrumentationOptions>(
        builder.Configuration.GetSection("AspNetCoreInstrumentation")
    );

// Logging
// builder.Logging.ClearProviders();

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(resourceBuilder);

    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
    });

#if DEBUG
    // options.AddConsoleExporter();
#endif
});

builder.Services.Configure<OpenTelemetryLoggerOptions>(opt =>
{
    opt.IncludeScopes = true;
    opt.ParseStateValues = true;
    opt.IncludeFormattedMessage = true;
});

// Metrics
var domainMeterName = "domain"; // same as cacheName

builder.Services.AddOpenTelemetry().WithMetrics(meterBuilder =>
{
    meterBuilder.SetResourceBuilder(resourceBuilder)
        .AddMeter(domainMeterName)
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation();

    meterBuilder.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
    });
#if DEBUG
    // options.AddConsoleExporter();
#endif
});


// Add services to the container.
var memoryCache = new MemoryCache(new MemoryCacheOptions());
builder.Services.AddSingleton<IMemoryCache>(memoryCache);
builder.Services.AddSingleton<IFusionCachePlugin>(
    new FusionMeter(
        domainMeterName, 
        memoryCache)
    );

builder.Services.AddFusionCache(options =>
    options.DefaultEntryOptions = new FusionCacheEntryOptions
    {
        Duration = TimeSpan.FromSeconds(60)
    }
        .SetFailSafe(true, TimeSpan.FromHours(1), TimeSpan.FromSeconds(5))
        .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1))
);


builder.Services.AddSingleton<IDataManager>(new DataManager("MockDomainCertData.json", "MockEmailToIpData.json"));
builder.Services.AddHostedService<DataFileMonitorService>();
builder.Services.AddSingleton(new DomainServiceConfig());

builder.Services.AddHostedService(sp => 
    new SwitchboardService(
        "../switchboard/switchboard.json", 
        sp.GetRequiredService<DomainServiceConfig>(),
        sp.GetService<ILogger<SwitchboardService>>()));

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
