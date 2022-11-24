using System.Text.Json;
using FusionCache.Example.Domain.Model;

namespace Services
{
    public interface IDataManager
    {
        Task<DomainCertData?>? GetDomain(string name, CancellationToken cancellationToken);
        Task<EmailToIpData?>? GetEmailRoute(string name, CancellationToken cancellationToken);
        string DomainCertDataPath { get; }
        string EmailToIpDataPath { get; }
        void LoadData<T>(string path);
    }

    public class DataManager : IDataManager
    {
        private List<DomainCertData>? domains;
        private List<EmailToIpData>? emailToIpData;

        public DataManager(string domainCertDataPath, string emailToIpDataPath)
        {
            DomainCertDataPath = domainCertDataPath;
            EmailToIpDataPath = emailToIpDataPath;

            LoadData<DomainCertData>(DomainCertDataPath);
            LoadData<EmailToIpData>(EmailToIpDataPath);
        }

        public void LoadData<T>(string path)
        {
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var data = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), serializeOptions);

            if (typeof(T) == typeof(DomainCertData))
            {
                domains = JsonSerializer.Deserialize<List<DomainCertData>>(File.ReadAllText(path), serializeOptions);
            }

            if (typeof(T) == typeof(EmailToIpData))
            {
                emailToIpData = JsonSerializer.Deserialize<List<EmailToIpData>>(File.ReadAllText(path), serializeOptions);
            }
        }

        public string DomainCertDataPath { get; } 
        public string EmailToIpDataPath { get; }


        public async Task<DomainCertData?> GetDomain(string name, CancellationToken ct)
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