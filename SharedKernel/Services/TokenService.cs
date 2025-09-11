using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using SharedKernel.Models;
using SharedKernel.Utility;

namespace SharedKernel.Services
{
    public interface ITokenService //register as Singleton
    {
        public Task<string> GetAccessTokenAsync(List<string> scopes);
    }
    public class TokenService : ITokenService
    {
        private readonly IConfidentialClientApplication _app;
        private readonly IDistributedCache _memoryCache;
        private readonly AzureADConfig _azureADConfig;
        public TokenService(IOptions<AzureADConfig> azureADConfig, IDistributedCache memoryCache)
        {
            _azureADConfig = azureADConfig.Value;
            _memoryCache = memoryCache;
            _app = ConfidentialClientApplicationBuilder
                .Create(_azureADConfig.MasterClientId)
                .WithClientSecret(_azureADConfig.MasterClientSecret)
                .WithAuthority(new Uri($"{_azureADConfig.Instance}{_azureADConfig.TenantId}"))
                .Build();

        }
        public async Task<string> GetAccessTokenAsync(List<string> scopes)
        {
            var appTokenInfo = await _memoryCache.GetAsync<AppTokenInfo>(_azureADConfig.MasterClientId);

            if (appTokenInfo == null || appTokenInfo.ExpiresOn <= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                var result = await _app.AcquireTokenForClient(scopes).ExecuteAsync();
                appTokenInfo = new AppTokenInfo { AccessToken = result.AccessToken, ExpiresOn = result.ExpiresOn };
                await _memoryCache.SetAsync(_azureADConfig.MasterClientId, appTokenInfo, new DistributedCacheEntryOptions());
            }

            return appTokenInfo.AccessToken;
        }

    }

    public class AppTokenInfo
    {
        public string AccessToken { get; set; } = null!;
        public DateTimeOffset? ExpiresOn { get; set; }
    }
}
