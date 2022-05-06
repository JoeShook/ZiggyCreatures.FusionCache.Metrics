using ZiggyCreatures.Caching.Fusion;

namespace EmailRouteService.Services;

public class DnsServiceCache
{
  
    public DnsServiceCache(IFusionCache fusionCache)
    {
        FusionCache = fusionCache;
    }

    public IFusionCache FusionCache { get; }
}