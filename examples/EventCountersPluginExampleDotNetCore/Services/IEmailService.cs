using System.Threading;
using System.Threading.Tasks;
using FusionCache.Example.Domain.Model;

namespace EventCountersPluginExampleDotNetCore.Services
{
    public interface IEmailService
    {
        Task<EmailToIpData> GetEmailRoute(string emailAddress, CancellationToken cancellationToken);
    }
}