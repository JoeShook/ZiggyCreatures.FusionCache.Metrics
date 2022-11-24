namespace Services.Model
{
    public class Switchboard
    {
        public DnsServiceConfig DnsServiceConfig { get; set; }
        public DomainServiceConfig DomainServiceConfig { get; set; }
        public EmailRouteServiceConfig EmailRouteServiceConfig { get; set; }
    }
}
