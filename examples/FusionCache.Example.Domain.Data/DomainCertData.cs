namespace FusionCache.Example.Domain.Model
{
    /// <summary>
    /// Mock data view after some database joins
    /// </summary>
    public class DomainCertData
    {
        /// <summary>
        /// Unique Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the DomainCertData
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Data is fake.  Not a real certificate
        /// </summary>
        public string Certificate { get; set; }
        
        /// <summary>
        /// Is this a valid and enabled Domain
        /// </summary>
        public bool Enabled { get; set; }
    }
}
