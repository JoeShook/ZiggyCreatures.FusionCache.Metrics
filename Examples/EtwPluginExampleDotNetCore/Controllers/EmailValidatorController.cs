using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using FusionCache.Example.Domain.Model;
using ZiggyCreatures.Caching.Fusion;

namespace EtwPluginExampleDotNetCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmailValidatorController : ControllerBase
    {
        private readonly DataManager _dataManager;
        private readonly IFusionCache _cache;

        public EmailValidatorController(DataManager dataManager, IFusionCache cache = null)
        {
            _dataManager = dataManager;
            _cache = cache;
        }

        [Route("EmailRoute/{emailAddress}")]
        [HttpGet]
        public async Task<IActionResult> GetEmailRoute([FromRoute] string emailAddress, CancellationToken cancellationToken)
        {
            EmailToIpData result;

            if (_cache != null)
            {
                var domainName = new MailAddress(emailAddress).Host;
                var domain = await _cache.GetOrSetAsync(domainName, await _dataManager.GetDomain(domainName, cancellationToken));
                
                if (domain != null && domain.Enabled)
                {
                    result = await _cache.GetOrSetAsync(emailAddress, await _dataManager.GetEmailRoute(emailAddress, cancellationToken));
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                result = await _dataManager.GetEmailRoute(emailAddress, cancellationToken);
            }

            return Ok(result);
        }

        [Route("Domain/{domainName}")]
        [HttpGet]
        public async Task<IActionResult> GetDomain([FromRoute] string domainName, CancellationToken cancellationToken)
        {
            DomainCertData result;

            if (_cache != null)
            {
                result = await _cache.GetOrSetAsync(domainName, await _dataManager.GetDomain(domainName, cancellationToken));
            }
            else
            {
                result = await _dataManager.GetDomain(domainName, cancellationToken);
            }

            return Ok(result);
        }
    }
}
 
