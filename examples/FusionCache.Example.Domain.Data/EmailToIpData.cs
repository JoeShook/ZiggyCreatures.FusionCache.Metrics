namespace FusionCache.Example.Domain.Model
{
    /// <summary>
    /// Mock data view after some interesting network resolution from domain to
    /// MX record to A record with a ttl (time to live)
    /// This is a simple view that only has one IP.
    /// </summary>
    public class EmailToIpData
    {
        /// <summary>
        /// Unique Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Email address of the EmailToIpData
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Represents an A record IP address for and SMTP server
        /// </summary>
        public string SmtpIp { get; set; }

        /// <summary>
        /// Time-to-live.  How long should this object be cached for.
        /// </summary>
        public int Ttl { get; set; }
    }
}