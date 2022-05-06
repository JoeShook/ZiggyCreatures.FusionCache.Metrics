using System.Net.Mail;
using EmailRouteService.Services;
using FusionCache.Example.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using ZiggyCreatures.Caching.Fusion;

namespace EmailRouteService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailValidatorController : ControllerBase
    {
        private readonly IFusionCache? _domainCache;
        private readonly DnsServiceCache _dnsServiceCache;
        private readonly DnsService _dnsService;
        private readonly DomainService _domainService;
        private readonly ILogger<EmailValidatorController> _logger;
        

        public EmailValidatorController(
            DnsService dnsService, 
            DomainService domainService,
            ILogger<EmailValidatorController> logger,
            IFusionCache? cache = null,
            DnsServiceCache? dnsServiceCache = null)
        {
            _domainCache = cache;
            _dnsServiceCache = dnsServiceCache;
            _dnsService = dnsService;
            _domainService = domainService;
            _logger = logger;

        }


        [Route("EmailRoute/{emailAddress}")]
        [HttpGet]
        public async Task<IActionResult?> GetEmailRoute([FromRoute] string emailAddress, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogInformation("Cancelled by cancellationToken");
                return null;
            }

            EmailToIpData? result;
            var hostPart = GetHostPart(emailAddress);

            if (hostPart == null)
            {
                return BadRequest("Invalid email address.");
            }
            
            if (_domainCache != null)
            {
                var domain = await _domainCache.GetOrSetAsync(
                    hostPart,
                    _ => _domainService.GetDomainCertData(hostPart, _), 
                    token: ct);
                
                if (domain != null && domain.Enabled)
                {
                    // Adaptive Caching.  Duration set by ttl of data from dns service
                    result = await _dnsServiceCache.FusionCache.GetOrSetAsync(
                        emailAddress, async (ctx, ct) =>
                        {
                            var emailToIpData = await _dnsService.GetDnsData(emailAddress, ct);
                            if (emailToIpData != null)
                            {
                                ctx.Options.SetDurationMs(emailToIpData.Ttl);
                            }

                            return emailToIpData;
                        },
                        options => options.SetDuration(TimeSpan.FromSeconds(10)) // DEFAULT: 10 SEC
                    );
                }
                else
                {
                    _logger.LogInformation("{emailAddress} not found", emailAddress);
                    return NotFound();
                }
            }
            else
            {
                var domain = await _domainService.GetDomainCertData(hostPart, ct);

                if (domain != null && domain.Enabled)
                {
                    result = await _dnsService.GetDnsData(emailAddress, ct);
                }
                else
                {
                    _logger.LogInformation("{emailAddress} not found", emailAddress);
                    return NotFound();
                }
            }

            return Ok(result);
        }

        private string? GetHostPart(string emailAddress)
        {
            try
            {
                return new MailAddress(emailAddress).Host;
            }
            catch
            {
                _logger.LogError("Invalid emailAddress: {emailAddress}", emailAddress);
            }

            return null;
        }

        [Route("Domain/{domainName}")]
        [HttpGet]
        public async Task<IActionResult> GetDomain([FromRoute] string domainName, CancellationToken ct)
        {
            DomainCertData? result;

            if (_domainCache != null)
            {
                result = await _domainCache.GetOrSetAsync(
                    domainName, 
                    _ => _domainService.GetDomainCertData(domainName, _), 
                    token: ct);
            }
            else
            {
                result = await _domainService.GetDomainCertData(domainName, ct);
            }

            if (result == null)
            {
                _logger.LogInformation($"{domainName} not found");
                return NotFound();
            }

            return Ok(result);
        }
    }
}
 
