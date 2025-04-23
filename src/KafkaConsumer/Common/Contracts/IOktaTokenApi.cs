using KafkaConsumer.Common.Models;
using Refit;
using System.Threading.Tasks;

namespace KafkaConsumer.Common.Contracts;

/// <summary>
/// Refit interface for interacting with the Okta token endpoint
/// </summary>
public interface IOktaTokenApi
{
    [Post("/v1/token")]
    Task<OktaTokenResponse> GetTokenAsync([Body(BodySerializationMethod.UrlEncoded)] OktaTokenRequest request);
} 