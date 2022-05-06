using System.Text.Json;
using FusionCache.Example.Domain.Model;

namespace TelemetryExampleServices
{
    public interface IDataManager
    {
        Task<DomainCertData?>? GetDomain(string name, CancellationToken cancellationToken);
        Task<EmailToIpData?>? GetEmailRoute(string name, CancellationToken cancellationToken);
    }

    public class DataManager : IDataManager
    {
        private readonly List<DomainCertData?> domains;
        private readonly List<EmailToIpData?> emailToIpData;

        public DataManager()
        {
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            domains = JsonSerializer.Deserialize<List<DomainCertData>>(File.ReadAllText("MockDomainCertData.json"), serializeOptions);
            emailToIpData = JsonSerializer.Deserialize<List<EmailToIpData>>(File.ReadAllText("MockEmailToIpData.json"), serializeOptions);
        }

        public async Task<DomainCertData?>? GetDomain(string name, CancellationToken ct)
        {
            int number = RandomGenerator.Next();

            if (number is > 40 and < 80) // cause some FACTORY_ERROR events.
            {
                throw new Exception("poof");
            }

            if (number is > 1400 and < 1460) // cause some STALE_REFRESH_ERROR events.
            {
                await Task.Delay(number, ct);
                throw new Exception("poof");
            }


            await Task.Delay(number, ct);
            // Console.WriteLine(number);

            if (ct.IsCancellationRequested)
            {
                return null;
            }

            return domains.SingleOrDefault(d => d.Domain.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<EmailToIpData?>? GetEmailRoute(string name, CancellationToken ct)
        {
            int number = RandomGenerator.Next();

            if (number is > 40 and < 80) // cause some FACTORY_ERROR events.
            {
                throw new Exception("poof");
            }

            if (number is > 1400 and < 1460) // cause some STALE_REFRESH_ERROR events.
            {
                await Task.Delay(number, ct);
                throw new Exception("poof");
            }
            
            await Task.Delay(number, ct);
            // Console.WriteLine(number);
            

            if (ct.IsCancellationRequested)
            {
                return null;
            }

            return emailToIpData.SingleOrDefault(d => d.Email.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}