using FusionCache.Example.Domain.Model;
using Microsoft.Net.Http.Headers;

namespace EmailRouteService.Services
{
    public class DnsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DnsService> _logger;

        public DnsService(HttpClient httpClient, ILogger<DnsService> logger)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("http://localhost:5245/");
            _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");

            _logger = logger;
        }

        public async Task<EmailToIpData?> GetDnsData(string emailAddress, CancellationToken ct = default)
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogInformation("Cancelled by cancellationToken");
                return null;
            }

            return await _httpClient.GetFromJsonAsync<EmailToIpData>($"api/Smtp/{emailAddress}", ct);
        }
    }
}