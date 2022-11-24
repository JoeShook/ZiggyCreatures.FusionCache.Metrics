using FusionCache.Example.Domain.Model;
using Microsoft.Net.Http.Headers;

namespace EmailRouteService.Services
{
    public class DomainService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DomainService> _logger;
        public DomainService(HttpClient httpClient, ILogger<DomainService> logger)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("http://localhost:5298/");
            _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");

            _logger = logger;
        }

        public async Task<DomainCertData?> GetDomainCertData(string hostName, CancellationToken ct = default)
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogInformation("Cancelled by cancellationToken");
                return null;
            }

            try
            {
                return await _httpClient.GetFromJsonAsync<DomainCertData>($"api/Domain/{hostName}", ct);
            }
            catch(Exception ex)
            {
                _logger.LogInformation(ex, "");

                return null;
            }
        }
    }
}