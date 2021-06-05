using System.Threading;
using System.Threading.Tasks;
using EtwPluginExampleDotNetCore.Controllers;
using FusionCache.Example.Domain.Model;
using ZiggyCreatures.Caching.Fusion;

namespace EtwPluginExampleDotNetCore.Services
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
            return await _cache.GetOrSetAsync(
                emailAddress, 
                await _dataManager.GetEmailRoute(emailAddress, cancellationToken),
                token: cancellationToken);
        }
    }
}
