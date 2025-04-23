using System.Threading.Tasks;

namespace KafkaConsumer.Common.Contracts;

/// <summary>
/// Interface for providing Okta tokens
/// </summary>
public interface IOktaTokenProvider
{
    /// <summary>
    /// Gets a valid Okta access token
    /// </summary>
    /// <returns>A valid access token</returns>
    Task<string> GetTokenAsync();
} 