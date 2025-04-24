using KafkaConsumer.Common.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;

namespace KafkaConsumer.IntegrationTests.Fixtures;

/// <summary>
/// Specialized KafkaConsumerTestFixture that mocks all external HTTP calls
/// </summary>
public class MockedHttpKafkaConsumerTestFixture : KafkaConsumerTestFixture
{
    /// <summary>
    /// Mock HTTP handler that will capture and return predefined responses
    /// </summary>
    public Mock<HttpMessageHandler> MockHttpHandler { get; } = new Mock<HttpMessageHandler>();
    
    /// <summary>
    /// Collection of captured HTTP requests
    /// </summary>
    public List<HttpRequestMessage> CapturedRequests { get; } = new List<HttpRequestMessage>();
    
    /// <summary>
    /// Override to configure test services with mocked HTTP handler
    /// </summary>
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);
        
        // Configure the mock handler to capture requests and return success by default
        MockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => CapturedRequests.Add(req))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"success\": true}")
            });

        // Replace the IOktaTokenApi with a mocked implementation
        services.AddTransient<IOktaTokenApi>(_ =>
        {
            var client = new HttpClient(MockHttpHandler.Object)
            {
                BaseAddress = new Uri("https://test.okta.com")
            };
            return Refit.RestService.For<IOktaTokenApi>(client);
        });
        
        // Replace all HTTP client factory registrations with mocked versions
        services.AddHttpClient("test-http-client")
            .ConfigurePrimaryHttpMessageHandler(() => MockHttpHandler.Object);
        
        // Replace the order API client with a mocked version
        services.AddTransient<IOrderApi>(_ =>
        {
            var client = new HttpClient(MockHttpHandler.Object)
            {
                BaseAddress = new Uri("https://test.example.com")
            };
            return Refit.RestService.For<IOrderApi>(client);
        });
    }
    
    /// <summary>
    /// Configure the mock handler to return a specific response for a specific request
    /// </summary>
    public void SetupMockResponse(Func<HttpRequestMessage, bool> requestMatcher, HttpResponseMessage response)
    {
        MockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => requestMatcher(req)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
    
    /// <summary>
    /// Configure the mock handler to return a specific string response for a specific request
    /// </summary>
    public void SetupMockJsonResponse(Func<HttpRequestMessage, bool> requestMatcher, string jsonContent, 
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        SetupMockResponse(requestMatcher, new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
        });
    }
} 