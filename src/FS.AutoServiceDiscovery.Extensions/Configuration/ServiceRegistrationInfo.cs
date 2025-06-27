using Microsoft.Extensions.DependencyInjection;
using FS.AutoServiceDiscovery.Extensions.Attributes;

namespace FS.AutoServiceDiscovery.Extensions.Configuration;

/// <summary>
/// Represents service registration information collected during the discovery process.
/// This class contains all the metadata needed to register a service with the dependency injection container.
/// Making this public allows external tools and extensions to work with service registration data.
/// </summary>
public class ServiceRegistrationInfo
{
    /// <summary>
    /// Gets or sets the service type that will be registered in the DI container.
    /// This is typically an interface type (e.g., IUserService) but can be a concrete type.
    /// </summary>
    public Type ServiceType { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the implementation type that provides the service functionality.
    /// This is the concrete class that implements the service interface.
    /// </summary>
    public Type ImplementationType { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the service lifetime for dependency injection.
    /// Determines how long the service instance will live (Singleton, Scoped, or Transient).
    /// </summary>
    public ServiceLifetime Lifetime { get; set; }
    
    /// <summary>
    /// Gets or sets the registration order for the service.
    /// Services with lower order values are registered first, which can be important
    /// for services that depend on registration order.
    /// </summary>
    public int Order { get; set; }
    
    /// <summary>
    /// Gets or sets the profile for which this service should be registered.
    /// This corresponds to the Profile property in ServiceRegistrationAttribute.
    /// </summary>
    public string? Profile { get; set; }
    
    /// <summary>
    /// Gets or sets whether this service should be ignored in test environments.
    /// This corresponds to the IgnoreInTests property in ServiceRegistrationAttribute.
    /// </summary>
    public bool IgnoreInTests { get; set; }
    
    /// <summary>
    /// Gets or sets the conditional attributes that apply to this service.
    /// These determine whether the service should be registered based on configuration values.
    /// </summary>
    public ConditionalServiceAttribute[] ConditionalAttributes { get; set; } = [];
}