namespace FusionCache.Example.Domain.Model
{
    /// <summary>
    /// Mock data view after some database joins
    /// </summary>
    public class DomainCertData
    {
        public int Id { get; set; }

        public string Domain { get; set; }

        /// <summary>
        /// Data is fake.  Not a real certificate
        /// </summary>
        public string Certificate { get; set; }
        
        public bool Enabled { get; set; }
    }
}
