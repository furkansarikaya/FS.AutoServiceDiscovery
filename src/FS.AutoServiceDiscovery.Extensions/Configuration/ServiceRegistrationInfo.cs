using Microsoft.Extensions.DependencyInjection;

namespace FS.AutoServiceDiscovery.Extensions.Configuration;

/// <summary>
/// Internal helper class that holds service registration information during the discovery process.
/// This class is used internally by the service discovery mechanism to collect and organize
/// service registration details before they are registered with the DI container.
/// </summary>
internal class ServiceRegistrationInfo
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
}