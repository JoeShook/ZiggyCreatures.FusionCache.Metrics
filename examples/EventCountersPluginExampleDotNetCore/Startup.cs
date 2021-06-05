using System;
using System.Text.Json;
using EventCountersPluginExampleDotNetCore.Services;
using JoeShook.FusionCache.EventCounters.Plugin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using ZiggyCreatures.Caching.Fusion;

namespace EventCountersPluginExampleDotNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = true;
            });

            services.AddSingleton(new DataManager());
            services.AddSingleton<IEmailService, EmailService>();

            //
            // Once this is a Fusion Cache Plugin maybe we can just call services.AddFusionCache(...)
            //
            var domainMemoryCache = new MemoryCache(new MemoryCacheOptions());
            services.AddSingleton<IMemoryCache>(domainMemoryCache);

            services.AddSingleton<IFusionCache>(serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<ZiggyCreatures.Caching.Fusion.FusionCache>>();

                var fusionCacheOptions = new FusionCacheOptions
                {
                    DefaultEntryOptions = new FusionCacheEntryOptions
                        {
                            Duration = TimeSpan.FromSeconds(5),
                            Priority = CacheItemPriority.High
                        }
                        .SetFailSafe(true, TimeSpan.FromSeconds(10))
                        .SetFactoryTimeouts(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(10))
                };

                // Future Plugin for hooking metrics ???
                var metrics = new FusionCacheEventSource("email", domainMemoryCache);
                var fusionCache = new ZiggyCreatures.Caching.Fusion.FusionCache(fusionCacheOptions, domainMemoryCache, logger);
                metrics.Wireup(fusionCache, fusionCacheOptions);

                return fusionCache;
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "EventCountersPluginExampleDotNetCore", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EtwPluginExampleDotNetCore v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
