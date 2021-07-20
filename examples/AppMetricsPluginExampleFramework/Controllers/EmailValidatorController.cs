using System.Net.Mail;
using System.Web.Http;
using AppMetricsPluginExample.Services;
using FusionCache.Example.Domain.Model;
using ZiggyCreatures.Caching.Fusion;

namespace AppMetricsPluginExample.Controllers
{
   
    public class EmailValidatorController : ApiController
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

        [HttpGet]
        public IHttpActionResult GetEmailRoute(string emailAddress)
        {
            EmailToIpData result;
            var hostPart = GetHostPart(emailAddress);

            if (hostPart == null)
            {
                return BadRequest("Invalid email address.");
            }

            if (_cache != null)
            {
                var domain = _cache.GetOrSet(
                    hostPart, 
                    _ => _dataManager.GetDomain(hostPart));
                
                if (domain != null && domain.Enabled)
                {
                    result = _emailService.GetEmailRoute(emailAddress);
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                result = _dataManager.GetEmailRoute(emailAddress);
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
        public IHttpActionResult GetDomain(string domainName)
        {
            DomainCertData result;

            if (_cache != null)
            {
                result = _cache.GetOrSet(
                    domainName, 
                    _ => _dataManager.GetDomain(domainName));
            }
            else
            {
                result =  _dataManager.GetDomain(domainName);
            }

            return Ok(result);
        }
    }
}
 
