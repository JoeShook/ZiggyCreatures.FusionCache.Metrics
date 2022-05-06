using System.Diagnostics;
using FusionCache.Example.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using TelemetryExampleServices;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Plugins.Metrics.Core;

namespace DomainService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DomainController : ControllerBase
    {
        private readonly IDataManager _dataManager;
        private readonly IFusionCache? _cache;
        private readonly ILogger<DomainController> _logger;

        public DomainController(
            IDataManager dataManager,
            ILogger<DomainController> logger,
            IFusionCache? cache = null)
        {
            _dataManager = dataManager;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet("{hostName}")]
        public async Task<IActionResult> Get([FromRoute] string hostName, CancellationToken cancellationToken)
        {
            using var scope = _logger.BeginScope("{Id}", Guid.NewGuid().ToString("N"));
            var activity = Activity.Current;

            DomainCertData? domain;

            if (_cache != null)
            {
                domain = await _cache.GetOrSetAsync(
                    hostName, _ => _dataManager.GetDomain(hostName, _),
                    token: cancellationToken);
            }
            else
            {
                domain = await _dataManager.GetDomain(hostName, cancellationToken);
            }

            if (domain == null)
            {
                return NoContent();
            }

            if (!domain.Enabled)
            {
                activity?.AddTag("DomainDisabled", true);
                _logger.LogInformation(domain.ToString());
            }

            return Ok(domain);
        }
    }
}
