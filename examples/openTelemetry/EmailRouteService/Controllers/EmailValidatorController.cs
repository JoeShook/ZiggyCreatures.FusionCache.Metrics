using System.Net.Mail;
using FusionCache.Example.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using ZiggyCreatures.Caching.Fusion;

namespace EmailRouteService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmailValidatorController : ControllerBase
    {
        
        private readonly IFusionCache _cache;

        public EmailValidatorController(IFusionCache cache = null)
        {
            _cache = cache;
        }

        [Route("EmailRoute/{emailAddress}")]
        [HttpGet]
        public async Task<IActionResult> GetEmailRoute([FromRoute] string emailAddress, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            EmailToIpData result;
            var hostPart = GetHostPart(emailAddress);

            if (hostPart == null)
            {
                return BadRequest("Invalid email address.");
            }

            // if (_cache != null)
            // {
            //     var domain = await _cache.GetOrSetAsync(
            //         hostPart,
            //         _ => _dataManager.GetDomain(hostPart, _), 
            //         token: cancellationToken);
            //     
            //     if (domain != null && domain.Enabled)
            //     {
            //         result = await _emailService.GetEmailRoute(emailAddress, cancellationToken);
            //     }
            //     else
            //     {
            //         return NotFound();
            //     }
            // }
            // else
            // {
            //     result = await _dataManager.GetEmailRoute(emailAddress, cancellationToken);
            // }

            return Ok();
        }

        private static string GetHostPart(string emailAddress)
        {
            try
            {
                return new MailAddress(emailAddress).Host;
            }
            catch
            {
                //log
            }

            return null;
        }

        [Route("Domain/{domainName}")]
        [HttpGet]
        public async Task<IActionResult> GetDomain([FromRoute] string domainName, CancellationToken cancellationToken)
        {
            DomainCertData result;

            // if (_cache != null)
            // {
            //     result = await _cache.GetOrSetAsync(
            //         domainName, 
            //         _ => _dataManager.GetDomain(domainName, _), 
            //         token: cancellationToken);
            // }
            // else
            // {
            //     result = await _dataManager.GetDomain(domainName, cancellationToken);
            // }

            return Ok();
        }
    }
}
 
