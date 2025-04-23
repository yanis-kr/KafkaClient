using KafkaConsumer.Common.Contracts;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaConsumer.Common.Services;

/// <summary>
/// HTTP message handler that adds Okta authentication to outgoing requests
/// </summary>
public class OktaAuthenticationHandler : DelegatingHandler
{
    private readonly IOktaTokenProvider _tokenProvider;

    public OktaAuthenticationHandler(IOktaTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // Get the token
        var token = await _tokenProvider.GetTokenAsync();
        
        // Add the Authorization header with the Bearer token
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Continue with the request pipeline
        return await base.SendAsync(request, cancellationToken);
    }
} 