using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using AppMetricsPluginExample;
using FusionCache.Example.Domain.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AppMetricsPluginExample2.Services
{
    public class DataManager
    {
        private List<DomainCertData> domains;
        private List<EmailToIpData> emailToIpDatas;

        public DataManager()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var path = HttpContext.Current.Server.MapPath("~");
            domains = JsonConvert.DeserializeObject<List<DomainCertData>>(
                File.ReadAllText(Path.Combine(path, "MockDomainCertData.json")), settings);

            emailToIpDatas = JsonConvert.DeserializeObject<List<EmailToIpData>>(
                File.ReadAllText(Path.Combine(path, "MockEmailToIpData.json")), settings);
        }

        public DomainCertData GetDomain(string name)
        {
            int number = RandomGenerator.Next();
            Thread.Sleep(number);

            return domains.SingleOrDefault(d => d.Domain.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public EmailToIpData GetEmailRoute(string name)
        {
            int number = RandomGenerator.Next();
            Thread.Sleep(number);

            return emailToIpDatas.SingleOrDefault(d => d.Email.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}