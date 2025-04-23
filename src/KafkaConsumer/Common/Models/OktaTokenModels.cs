using System;
using System.Text.Json.Serialization;

namespace KafkaConsumer.Common.Models;

/// <summary>
/// Request model for getting a token from Okta
/// </summary>
public class OktaTokenRequest
{
    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; } = "client_credentials";

    [JsonPropertyName("client_id")]
    public string ClientId { get; set; }

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = "default";
}

/// <summary>
/// Response model from Okta token endpoint
/// </summary>
public class OktaTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; }

    [JsonIgnore]
    public DateTimeOffset ExpirationTime { get; set; }
} 