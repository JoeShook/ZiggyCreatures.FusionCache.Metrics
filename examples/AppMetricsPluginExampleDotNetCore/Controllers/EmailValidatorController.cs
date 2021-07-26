using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using AppMetricsPluginExampleDotNetCore.Services;
using FusionCache.Example.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using ZiggyCreatures.Caching.Fusion;

namespace AppMetricsPluginExampleDotNetCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmailValidatorController : ControllerBase
    {
        private readonly DataManager _dataManager;
        private readonly IEmailService _emailService;
        private readonly IFusionCache _cache;

        public EmailValidatorController(DataManager dataManager, IEmailService emailService, IFusionCache cache = null)
        {
            _dataManager = dataManager;
            _emailService = emailService;
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

            if (_cache != null)
            {
                try
                {
                    var domain = await _cache.GetOrSetAsync(
                        hostPart,
                        _ => _dataManager.GetDomain(hostPart, _),
                        token: cancellationToken);

                    if (domain != null && domain.Enabled)
                    {
                        result = await _emailService.GetEmailRoute(emailAddress, cancellationToken);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
                catch (Exception ex)
                {
                    //log
                    
                    return NotFound(ex.ToString());
                }
            }
            else
            {
                result = await _dataManager.GetEmailRoute(emailAddress, cancellationToken);
            }

            return Ok(result);
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

            if (_cache != null)
            {
                result = await _cache.GetOrSetAsync(
                    domainName, 
                    _ => _dataManager.GetDomain(domainName, _), 
                    token: cancellationToken);
            }
            else
            {
                result = await _dataManager.GetDomain(domainName, cancellationToken);
            }

            return Ok(result);
        }
    }
}
 
