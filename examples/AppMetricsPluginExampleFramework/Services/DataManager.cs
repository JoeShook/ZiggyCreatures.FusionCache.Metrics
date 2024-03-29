﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using FusionCache.Example.Domain.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AppMetricsPluginExample.Services
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

            if (number > 40 && number < 80) // cause some FACTORY_ERROR events.
            {
                throw new Exception("poof");
            }

            if (number > 1400 && number < 1460) // cause some STALE_REFRESH_ERROR events.
            {
                Thread.Sleep(number);
                throw new Exception("poof");
            }


            Thread.Sleep(number);
            // Console.WriteLine(number);
            
            return domains.SingleOrDefault(d => d.Domain.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public EmailToIpData GetEmailRoute(string name)
        {
            int number = RandomGenerator.Next();

            if (number > 40 && number < 80) // cause some FACTORY_ERROR events.
            {
                throw new Exception("poof");
            }

            if (number > 1400 && number < 1460) // cause some STALE_REFRESH_ERROR events.
            {
                Thread.Sleep(number);
                throw new Exception("poof");
            }

            Thread.Sleep(number);
            // Console.WriteLine(number);

            return emailToIpDatas.SingleOrDefault(d => d.Email.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}