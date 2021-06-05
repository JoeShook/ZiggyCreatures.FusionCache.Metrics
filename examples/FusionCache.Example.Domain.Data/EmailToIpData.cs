namespace FusionCache.Example.Domain.Model
{
    /// <summary>
    /// Mock data view after some interesting network resolution from domain to
    /// MX record to A record with a ttl (time to live)
    /// This is a simple view that only has one IP.
    /// </summary>
    public class EmailToIpData
    {
        public int Id { get; set; }

        public string Email { get; set; }

        public string SmtpIp { get; set; }

        public int Ttl { get; set; }
    }
}