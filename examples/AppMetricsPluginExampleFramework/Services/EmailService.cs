﻿using FusionCache.Example.Domain.Model;
using ZiggyCreatures.Caching.Fusion;

namespace AppMetricsPluginExample.Services
{
    public interface IEmailService
    {
        EmailToIpData GetEmailRoute(string emailAddress);
    }

    public class EmailService : IEmailService
    {
        private readonly DataManager _dataManager;
        private readonly IFusionCache _cache;

        public EmailService(DataManager dataManager, IFusionCache cache)
        {
            _dataManager = dataManager;
            _cache = cache;
        }


        public EmailToIpData GetEmailRoute(string emailAddress)
        {
            return _cache.GetOrSet(
                emailAddress, 
                _ => _dataManager.GetEmailRoute(emailAddress));
        }
    }
}
