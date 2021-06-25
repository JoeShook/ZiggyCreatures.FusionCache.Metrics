using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FusionCache.Example.Domain.Model;

namespace EventCountersPluginExampleDotNetCore.Services
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
            int number = RandomGenerator.Next();
            await Task.Delay(number, cancellationToken);
            Console.WriteLine(number);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            return domains.SingleOrDefault(d => d.Domain.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<EmailToIpData> GetEmailRoute(string name, CancellationToken cancellationToken)
        {
            int number = RandomGenerator.Next();
            
            if (number > 1400)
            {
                await Task.Delay(number, cancellationToken);
                throw new Exception("poof");
            }
            
            await Task.Delay(number, cancellationToken);
            Console.WriteLine(number);
            

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            return emailToIpDatas.SingleOrDefault(d => d.Email.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}