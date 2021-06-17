using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FusionCache.Example.Domain.Model;
using ZiggyCreatures.Caching.Fusion;

namespace EventCountersPluginExampleDotNetCore.Services
{
    public class EmailService : IEmailService
    {
        private readonly DataManager _dataManager;
        private readonly IFusionCache _cache;

        public EmailService(DataManager dataManager, IFusionCache cache)
        {
            _dataManager = dataManager;
            _cache = cache;
        }


        public async Task<EmailToIpData> GetEmailRoute(string emailAddress, CancellationToken cancellationToken)
        {
            return await _cache.GetOrSetAsync(
                emailAddress,
                await _dataManager.GetEmailRoute(emailAddress, cancellationToken),
                token: cancellationToken);

            /*
            return await _cache.GetOrSetAsync(
                emailAddress, 
                await _dataManager.GetEmailRoute(emailAddress, cancellationToken),
                (options, EmailToIpData result) => options.SetDuration(TimeSpan.FromMilliseconds(result.Ttl)),
                token: cancellationToken);

            Or maybe MemoryOptionsModifier was going to let this happen
            */

            //
            // var result = await _cache.TryGetAsync<EmailToIpData>(emailAddress, token: cancellationToken);
            //
            // if(!result.HasValue)
            // {
            //     var email = await _dataManager.GetEmailRoute(emailAddress, cancellationToken);
            //
            //     if(email != null && email.Ttl > 0)
            //     {
            //         await _cache.SetAsync(emailAddress, email,
            //         options => options.Duration = TimeSpan.FromMilliseconds(email.Ttl),
            //         cancellationToken);
            //     }
            //     else
            //     {
            //         await _cache.SetAsync(emailAddress, email, token: cancellationToken);
            //     }
            //
            //     return email;
            // }
            //
            // return result;
        }
    }
}
