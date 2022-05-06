using Microsoft.AspNetCore.Mvc;
using TelemetryExampleServices;

namespace DnsService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SmtpController : ControllerBase
    {
        private readonly IDataManager _dataManager;
        private readonly ILogger<SmtpController> _logger;

        public SmtpController(IDataManager dataManager, ILogger<SmtpController> logger)
        {
            _dataManager = dataManager;
            _logger = logger;
        }

        [HttpGet("{emailAddress}")]
        public async Task<IActionResult> Get([FromRoute] string emailAddress, CancellationToken cancellationToken = default)
        {
            var result = await _dataManager.GetEmailRoute(emailAddress, cancellationToken);

            if (result == null)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
    }
}
