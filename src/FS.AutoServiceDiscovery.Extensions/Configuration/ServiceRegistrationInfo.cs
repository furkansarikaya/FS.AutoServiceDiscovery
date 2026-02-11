using Microsoft.Extensions.DependencyInjection;
using FS.AutoServiceDiscovery.Extensions.Attributes;

namespace FS.AutoServiceDiscovery.Extensions.Configuration;

/// <summary>
/// Represents service registration information collected during the discovery process.
/// This class contains all the metadata needed to register a service with the dependency injection container.
/// Making this public allows external tools and extensions to work with service registration data.
/// </summary>
/// <remarks>
/// This class is populated during the assembly scanning phase and consumed during the registration phase.
/// It supports keyed services via <see cref="ServiceKey"/> and duplicate prevention via <see cref="UseTryAdd"/>.
/// </remarks>
/// <seealso cref="Attributes.ServiceRegistrationAttribute"/>
/// <seealso cref="AutoServiceOptions"/>
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

    /// <summary>
    /// Gets or sets the service key for keyed service registration (.NET 8+).
    /// When specified, the service is registered as a keyed service.
    /// </summary>
    /// <remarks>
    /// Keyed services allow multiple implementations of the same interface
    /// to coexist in the DI container, differentiated by a unique key.
    /// </remarks>
    public object? ServiceKey { get; set; }

    /// <summary>
    /// Gets or sets whether to use TryAdd pattern for this service registration.
    /// When true, the service will only be registered if no existing registration
    /// for the same service type exists.
    /// </summary>
    /// <remarks>
    /// This prevents duplicate registrations in modular applications
    /// where multiple modules might register the same service.
    /// </remarks>
    public bool UseTryAdd { get; set; }

    /// <summary>
    /// Gets or sets whether this registration represents a decorator.
    /// Decorators are handled separately: they wrap an existing registration
    /// rather than creating a new independent registration.
    /// </summary>
    public bool IsDecorator { get; set; }

    /// <summary>
    /// Gets or sets the service type being decorated (only applicable when <see cref="IsDecorator"/> is true).
    /// </summary>
    public Type? DecoratedServiceType { get; set; }
}