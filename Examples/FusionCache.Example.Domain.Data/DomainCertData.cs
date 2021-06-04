namespace FusionCache.Example.Domain.Model
{
    /// <summary>
    /// Mock data view after some database joins
    /// </summary>
    public class DomainCertData
    {
        public int id { get; set; }

        public string domain { get; set; }

        /// <summary>
        /// Data is fake.  Not a real certificate
        /// </summary>
        public string certificate { get; set; }
        
    }
}
