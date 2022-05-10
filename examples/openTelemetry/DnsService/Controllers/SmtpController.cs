using System.Diagnostics;
using FusionCache.Example.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using TelemetryExampleServices;
using ZiggyCreatures.Caching.Fusion;

namespace DnsService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SmtpController : ControllerBase
    {
        private readonly IDataManager _dataManager;
        private readonly IFusionCache? _cache;
        private readonly ILogger<SmtpController> _logger;

        public SmtpController(
            IDataManager dataManager, 
            ILogger<SmtpController> logger,
            IFusionCache? cache = null)
        {
            _dataManager = dataManager;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet("{emailAddress}")]
        public async Task<IActionResult> Get([FromRoute] string emailAddress, CancellationToken cancellationToken = default)
        {
            using var scope = _logger.BeginScope("{Id}", Guid.NewGuid().ToString("N"));
            var activity = Activity.Current;

            EmailToIpData? resourceRecord;

            if (_cache != null)
            {
                resourceRecord = await _cache.GetOrSetAsync(
                    emailAddress, async (ctx, _) =>
                    {
                        var emailToIpData = await _dataManager.GetEmailRoute(emailAddress, _)!;
                        if (emailToIpData != null)
                        {
                            ctx.Options.SetDurationMs(emailToIpData.Ttl);
                        }

                        return emailToIpData;
                    },
                    options => options.SetDuration(TimeSpan.FromSeconds(10)), // DEFAULT: 10 SEC
                    token: cancellationToken);
            }
            else
            {
                resourceRecord = await _dataManager.GetEmailRoute(emailAddress, cancellationToken)!;
            }

            if (resourceRecord == null)
            {
                return NotFound();
            }

            if (resourceRecord.Ttl > 5000)
            {
                activity?.AddTag("LargeTTL", true);
                _logger.LogInformation(resourceRecord.ToString());
            }

            return Ok(resourceRecord);
        }
    }
}
