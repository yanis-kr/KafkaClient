using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Services;
using Moq;
using Moq.Protected;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KafkaConsumer.Tests.Common.Services;

public class OktaAuthenticationHandlerTests
{
    private readonly Mock<IOktaTokenProvider> _mockTokenProvider;
    private readonly Mock<HttpMessageHandler> _mockInnerHandler;
    
    public OktaAuthenticationHandlerTests()
    {
        _mockTokenProvider = new Mock<IOktaTokenProvider>();
        _mockInnerHandler = new Mock<HttpMessageHandler>();
    }
    
    [Fact]
    public async Task SendAsync_ShouldAddAuthorizationHeader()
    {
        // Arrange
        const string token = "test-token";
        _mockTokenProvider.Setup(p => p.GetTokenAsync()).ReturnsAsync(token);
        
        var handler = new OktaAuthenticationHandler(_mockTokenProvider.Object)
        {
            InnerHandler = _mockInnerHandler.Object
        };
        
        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://test.com/api");
        
        _mockInnerHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => 
            {
                // Store the request for later verification
                request = req;
            });
        
        // Act
        await httpClient.SendAsync(request);
        
        // Assert
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Equal(token, request.Headers.Authorization.Parameter);
        
        // Verify that GetTokenAsync was called once
        _mockTokenProvider.Verify(p => p.GetTokenAsync(), Times.Once);
    }
    
    [Fact]
    public async Task SendAsync_ShouldPropagateExceptions()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Token provider error");
        _mockTokenProvider.Setup(p => p.GetTokenAsync()).ThrowsAsync(expectedException);
        
        var handler = new OktaAuthenticationHandler(_mockTokenProvider.Object)
        {
            InnerHandler = _mockInnerHandler.Object
        };
        
        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://test.com/api");
        
        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => httpClient.SendAsync(request));
            
        Assert.Same(expectedException, actualException);
    }
} 