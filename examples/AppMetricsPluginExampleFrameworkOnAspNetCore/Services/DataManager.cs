using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FusionCache.Example.Domain.Model;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AppMetricsPluginExample.Services
{
    public class DataManager
    {
        private List<DomainCertData> domains;
        private List<EmailToIpData> emailToIpDatas;

        public DataManager()
        {
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            domains = JsonSerializer.Deserialize<List<DomainCertData>>(File.ReadAllText("MockDomainCertData.json"), serializeOptions);
            emailToIpDatas = JsonSerializer.Deserialize<List<EmailToIpData>>(File.ReadAllText("MockEmailToIpData.json"), serializeOptions);
        }

        public async Task<DomainCertData> GetDomain(string name, CancellationToken cancellationToken)
        {
            var rndWait = new Random();
            int number = rndWait.Next(10, 200);
            await Task.Delay(number);

            return domains.SingleOrDefault(d => d.Domain.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<EmailToIpData> GetEmailRoute(string name, CancellationToken cancellationToken)
        {
            var rndWait = new Random();
            int number = rndWait.Next(10, 200);
            await Task.Delay(number, cancellationToken);

            return emailToIpDatas.SingleOrDefault(d => d.Email.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}