using System.Threading;
using System.Threading.Tasks;
using FusionCache.Example.Domain.Model;
using ZiggyCreatures.Caching.Fusion;

namespace AppMetricsPluginExample.Services
{
    public interface IEmailService
    {
        Task<EmailToIpData> GetEmailRoute(string emailAddress, CancellationToken cancellationToken);
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

        public async Task<EmailToIpData> GetEmailRoute(string emailAddress, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            return await _cache.GetOrSetAsync(
                emailAddress, 
                _ => _dataManager.GetEmailRoute(emailAddress, _),
                token: cancellationToken);
        }
    }
}
