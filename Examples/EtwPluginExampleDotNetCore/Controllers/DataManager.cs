using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FusionCache.Example.Domain.Model;

namespace EtwPluginExampleDotNetCore.Controllers
{
    public class DataManager
    {
        private List<DomainCertData> domains;
        private List<EmailToIpData> emailToIpDatas;

        public DataManager()
        {
            domains = JsonSerializer.Deserialize<List<DomainCertData>>("./MockDomainCertData.json");
            emailToIpDatas = JsonSerializer.Deserialize<List<EmailToIpData>>("./MockEmailToIpData.json");
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