using System;

namespace KafkaConsumer.Common.Configuration;

public class OktaSettings
{
    /// <summary>
    /// Okta token endpoint
    /// </summary>
    public string TokenUrl { get; set; }
    
    /// <summary>
    /// Okta client ID
    /// </summary>
    public string ClientId { get; set; }
    
    /// <summary>
    /// Okta client secret
    /// </summary>
    public string ClientSecret { get; set; }
    
    /// <summary>
    /// Default token lifetime in seconds (default: 1 hour)
    /// </summary>
    public int DefaultTokenLifetimeSeconds { get; set; } = 3600;
} 