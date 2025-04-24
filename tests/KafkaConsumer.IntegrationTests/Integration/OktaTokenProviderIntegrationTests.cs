using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Models;
using KafkaConsumer.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace KafkaConsumer.IntegrationTests.Integration;

public class OktaTokenProviderIntegrationTests : IClassFixture<KafkaConsumerTestFixture>
{
    private readonly KafkaConsumerTestFixture _fixture;
    
    public OktaTokenProviderIntegrationTests(KafkaConsumerTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task IntegratedTokenProvider_GetToken_ShouldReturnValidToken()
    {
        // Arrange
        // Override the token API with a mock that returns a test token
        var mockHandler = new Mock<HttpMessageHandler>();
        var expectedToken = "test-integration-token-abc123";
        
        // Set up mock response
        var tokenResponse = new OktaTokenResponse
        {
            AccessToken = expectedToken,
            TokenType = "Bearer",
            ExpiresIn = 3600,
            Scope = "default"
        };
        
        var jsonResponse = JsonSerializer.Serialize(tokenResponse);
        
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });
        
        // Create service collection with mock
        var services = new ServiceCollection();
        
        // Add the token API with our mock handler
        services.AddTransient<IOktaTokenApi>(_ =>
        {
            var client = new HttpClient(mockHandler.Object)
            {
                BaseAddress = new Uri("https://test.okta.com")
            };
            return Refit.RestService.For<IOktaTokenApi>(client);
        });
        
        // Add other required services from the main fixture
        services.AddSingleton(_fixture.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<KafkaConsumer.Common.Configuration.OktaSettings>>());
        services.AddSingleton(_fixture.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<KafkaConsumer.Common.Services.OktaTokenProvider>>());
        
        // Add the token provider
        services.AddSingleton<IOktaTokenProvider, KafkaConsumer.Common.Services.OktaTokenProvider>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Get token provider from DI
        var tokenProvider = serviceProvider.GetRequiredService<IOktaTokenProvider>();
        
        // Act
        var token = await tokenProvider.GetTokenAsync();
        var secondToken = await tokenProvider.GetTokenAsync(); // This should use the cached token
        
        // Assert
        Assert.Equal(expectedToken, token);
        Assert.Equal(expectedToken, secondToken);
        
        // Verify the mock was called only once (second call should use cache)
        mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }
    
    [Fact]
    public async Task IntegratedAuthHandler_SendAsync_ShouldAddToken()
    {
        // Arrange
        // Create a request that will go through the auth handler
        var mockTokenProvider = new Mock<IOktaTokenProvider>();
        mockTokenProvider.Setup(p => p.GetTokenAsync()).ReturnsAsync("integration-auth-token");
        
        var mockInnerHandler = new Mock<HttpMessageHandler>();
        HttpRequestMessage capturedRequest = null;
        
        mockInnerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });
        
        // Create the auth handler with our mocks
        var authHandler = new KafkaConsumer.Common.Services.OktaAuthenticationHandler(mockTokenProvider.Object)
        {
            InnerHandler = mockInnerHandler.Object
        };
        
        var client = new HttpClient(authHandler);
        
        // Act
        await client.GetAsync("https://test.example.com/api/test");
        
        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization.Scheme);
        Assert.Equal("integration-auth-token", capturedRequest.Headers.Authorization.Parameter);
        
        // Verify token provider was called
        mockTokenProvider.Verify(p => p.GetTokenAsync(), Times.Once);
    }
} 