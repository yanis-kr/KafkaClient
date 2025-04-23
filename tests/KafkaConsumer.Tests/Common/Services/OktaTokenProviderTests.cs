using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Models;
using KafkaConsumer.Common.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace KafkaConsumer.Tests.Common.Services;

public class OktaTokenProviderTests
{
    private readonly Mock<IOktaTokenApi> _mockTokenApi;
    private readonly Mock<IOptions<OktaSettings>> _mockOptions;
    private readonly Mock<ILogger<OktaTokenProvider>> _mockLogger;
    private readonly OktaSettings _oktaSettings;
    
    public OktaTokenProviderTests()
    {
        _mockTokenApi = new Mock<IOktaTokenApi>();
        _mockLogger = new Mock<ILogger<OktaTokenProvider>>();
        _oktaSettings = new OktaSettings
        {
            TokenUrl = "https://test.okta.com/oauth2/v1/token",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            DefaultTokenLifetimeSeconds = 3600
        };
        
        _mockOptions = new Mock<IOptions<OktaSettings>>();
        _mockOptions.Setup(o => o.Value).Returns(_oktaSettings);
    }
    
    [Fact]
    public async Task GetTokenAsync_ShouldReturnCachedToken_WhenTokenIsNotExpired()
    {
        // Arrange
        var tokenResponse = new OktaTokenResponse
        {
            AccessToken = "test-token",
            ExpiresIn = 3600,
            TokenType = "Bearer",
            ExpirationTime = DateTimeOffset.UtcNow.AddSeconds(3600)
        };
        
        _mockTokenApi
            .Setup(api => api.GetTokenAsync(It.IsAny<OktaTokenRequest>()))
            .ReturnsAsync(tokenResponse);
            
        var tokenProvider = new OktaTokenProvider(
            _mockTokenApi.Object,
            _mockOptions.Object,
            _mockLogger.Object);
            
        // Act
        var firstToken = await tokenProvider.GetTokenAsync();
        var secondToken = await tokenProvider.GetTokenAsync();
        
        // Assert
        Assert.Equal("test-token", firstToken);
        Assert.Equal("test-token", secondToken);
        
        // Verify token API was called only once
        _mockTokenApi.Verify(
            api => api.GetTokenAsync(It.IsAny<OktaTokenRequest>()),
            Times.Once);
    }
    
    [Fact]
    public async Task GetTokenAsync_ShouldFetchNewToken_WhenTokenIsExpired()
    {
        // Arrange
        var expiredTokenResponse = new OktaTokenResponse
        {
            AccessToken = "expired-token",
            ExpiresIn = 1,
            TokenType = "Bearer",
            ExpirationTime = DateTimeOffset.UtcNow.AddSeconds(-30) // Already expired
        };
        
        var newTokenResponse = new OktaTokenResponse
        {
            AccessToken = "new-token",
            ExpiresIn = 3600,
            TokenType = "Bearer",
            ExpirationTime = DateTimeOffset.UtcNow.AddSeconds(3600)
        };
        
        // Setup the mock to return an expired token first, then a new token
        _mockTokenApi
            .SetupSequence(api => api.GetTokenAsync(It.IsAny<OktaTokenRequest>()))
            .ReturnsAsync(expiredTokenResponse)
            .ReturnsAsync(newTokenResponse);
            
        var tokenProvider = new OktaTokenProvider(
            _mockTokenApi.Object,
            _mockOptions.Object,
            _mockLogger.Object);
            
        // Act
        var firstToken = await tokenProvider.GetTokenAsync();
        
        // Simulate time passing
        await Task.Delay(10);
        
        var secondToken = await tokenProvider.GetTokenAsync();
        
        // Assert
        Assert.Equal("expired-token", firstToken);
        Assert.Equal("new-token", secondToken);
        
        // Verify token API was called twice
        _mockTokenApi.Verify(
            api => api.GetTokenAsync(It.IsAny<OktaTokenRequest>()),
            Times.Exactly(2));
    }
    
    [Fact]
    public async Task GetTokenAsync_ShouldUseCorrectCredentials()
    {
        // Arrange
        var tokenResponse = new OktaTokenResponse
        {
            AccessToken = "test-token",
            ExpiresIn = 3600,
            TokenType = "Bearer"
        };
        
        OktaTokenRequest capturedRequest = null;
        
        _mockTokenApi
            .Setup(api => api.GetTokenAsync(It.IsAny<OktaTokenRequest>()))
            .Callback<OktaTokenRequest>(req => capturedRequest = req)
            .ReturnsAsync(tokenResponse);
            
        var tokenProvider = new OktaTokenProvider(
            _mockTokenApi.Object,
            _mockOptions.Object,
            _mockLogger.Object);
            
        // Act
        await tokenProvider.GetTokenAsync();
        
        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("test-client-id", capturedRequest.ClientId);
        Assert.Equal("test-client-secret", capturedRequest.ClientSecret);
        Assert.Equal("client_credentials", capturedRequest.GrantType);
    }
} 