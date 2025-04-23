using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaConsumer.Common.Services;

/// <summary>
/// Service for providing Okta access tokens with caching
/// </summary>
public class OktaTokenProvider : IOktaTokenProvider
{
    private readonly IOktaTokenApi _tokenApi;
    private readonly ILogger<OktaTokenProvider> _logger;
    private readonly OktaSettings _oktaSettings;
    private OktaTokenResponse _cachedToken;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public OktaTokenProvider(
        IOktaTokenApi tokenApi,
        IOptions<OktaSettings> oktaOptions,
        ILogger<OktaTokenProvider> logger)
    {
        _tokenApi = tokenApi ?? throw new ArgumentNullException(nameof(tokenApi));
        _oktaSettings = oktaOptions?.Value ?? throw new ArgumentNullException(nameof(oktaOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a valid Okta access token, using cached token if it's still valid
    /// </summary>
    /// <returns>A valid access token</returns>
    public async Task<string> GetTokenAsync()
    {
        // Check if we have a cached token that's still valid (with 30 seconds buffer)
        if (_cachedToken != null && DateTimeOffset.UtcNow.AddSeconds(30) < _cachedToken.ExpirationTime)
        {
            return _cachedToken.AccessToken;
        }

        // Token is expired or not yet retrieved, get a new one
        await _semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring the lock
            if (_cachedToken != null && DateTimeOffset.UtcNow.AddSeconds(30) < _cachedToken.ExpirationTime)
            {
                return _cachedToken.AccessToken;
            }

            _logger.LogInformation("Fetching new Okta token");
            
            var request = new OktaTokenRequest
            {
                ClientId = _oktaSettings.ClientId,
                ClientSecret = _oktaSettings.ClientSecret
            };

            var response = await _tokenApi.GetTokenAsync(request);
            
            // Set the expiration time based on the token response
            response.ExpirationTime = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn);
            
            // Cache the token
            _cachedToken = response;
            
            return response.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Okta token");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }
} 