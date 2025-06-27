using Microsoft.Extensions.DependencyInjection;

namespace FS.AutoServiceDiscovery.Extensions.Attributes;

/// <summary>
/// Marks a class for automatic service registration in the dependency injection container.
/// This attribute enables convention-based service discovery and registration.
/// </summary>
/// <param name="lifetime">The service lifetime (Singleton, Scoped, or Transient)</param>
/// <example>
/// <code>
/// [ServiceRegistration(ServiceLifetime.Scoped)]
/// public class UserService : IUserService
/// {
///     // Service implementation
/// }
/// 
/// [ServiceRegistration(ServiceLifetime.Singleton, Order = 1, Profile = "Production")]
/// public class CacheService : ICacheService
/// {
///     // Service implementation
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ServiceRegistrationAttribute(ServiceLifetime lifetime) : Attribute
{
    /// <summary>
    /// Gets the service lifetime for dependency injection registration.
    /// </summary>
    public ServiceLifetime Lifetime { get; } = lifetime;
    
    /// <summary>
    /// Gets or sets the registration order. Services with lower order values are registered first.
    /// Default is 0.
    /// </summary>
    public int Order { get; set; } = 0;
    
    /// <summary>
    /// Gets or sets the explicit service type to register. 
    /// If not specified, the service type is determined by convention (I{ClassName} interface or implemented interfaces).
    /// </summary>
    public Type? ServiceType { get; set; }
    
    /// <summary>
    /// Gets or sets the profile for conditional registration (e.g., "Development", "Production").
    /// If specified, the service will only be registered when the matching profile is active.
    /// </summary>
    public string? Profile { get; set; }
    
    /// <summary>
    /// Gets or sets whether this service should be ignored in test environments.
    /// Default is false.
    /// </summary>
    public bool IgnoreInTests { get; set; } = false;
}