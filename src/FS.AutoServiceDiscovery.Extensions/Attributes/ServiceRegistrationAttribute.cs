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
/// <remarks>
/// This attribute supports keyed services (.NET 8+), TryAdd pattern for duplicate prevention,
/// and multiple interface registration via the <see cref="ServiceTypes"/> property.
/// For open generic registrations, use <see cref="OpenGenericRegistrationAttribute"/> instead.
/// </remarks>
/// <seealso cref="OpenGenericRegistrationAttribute"/>
/// <seealso cref="DecoratorServiceAttribute"/>
/// <seealso cref="ConditionalServiceAttribute"/>
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

    /// <summary>
    /// Gets or sets the service key for keyed service registration (.NET 8+).
    /// When specified, the service is registered as a keyed service using
    /// the keyed service registration APIs (AddKeyedScoped, AddKeyedSingleton, etc.).
    /// </summary>
    /// <example>
    /// <code>
    /// [ServiceRegistration(ServiceLifetime.Scoped, ServiceKey = "smtp")]
    /// public class SmtpEmailService : IEmailService { }
    ///
    /// [ServiceRegistration(ServiceLifetime.Scoped, ServiceKey = "sendgrid")]
    /// public class SendGridEmailService : IEmailService { }
    /// </code>
    /// </example>
    /// <remarks>
    /// Keyed services allow multiple implementations of the same interface
    /// to be registered and resolved by a unique key. This is useful for
    /// strategy pattern, multi-tenant scenarios, and feature toggles.
    /// Requires .NET 8.0 or later.
    /// </remarks>
    public object? ServiceKey { get; set; }

    /// <summary>
    /// Gets or sets whether to use TryAdd pattern for this service registration.
    /// When true, the service will only be registered if no existing registration
    /// for the same service type exists, preventing duplicate registrations.
    /// </summary>
    /// <remarks>
    /// This is useful in modular applications where multiple modules might attempt
    /// to register the same service. The first registration wins.
    /// Uses ServiceCollectionDescriptorExtensions.TryAdd internally.
    /// </remarks>
    public bool UseTryAdd { get; set; } = false;

    /// <summary>
    /// Gets or sets multiple service types for this implementation.
    /// When specified, the implementation is registered under each service type.
    /// </summary>
    /// <example>
    /// <code>
    /// [ServiceRegistration(ServiceLifetime.Scoped, ServiceTypes = new[] { typeof(IUserService), typeof(IProfileService) })]
    /// public class UserService : IUserService, IProfileService { }
    /// </code>
    /// </example>
    /// <remarks>
    /// When ServiceTypes is specified, the implementation is registered for each type in the array.
    /// The <see cref="ServiceType"/> property is ignored when ServiceTypes is set.
    /// </remarks>
    public Type[]? ServiceTypes { get; set; }
}