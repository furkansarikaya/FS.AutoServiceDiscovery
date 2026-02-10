using Microsoft.Extensions.DependencyInjection;

namespace FS.AutoServiceDiscovery.Extensions.Attributes;

/// <summary>
/// Marks an open generic class for automatic service registration.
/// This attribute enables registration of generic service implementations
/// such as Repository&lt;T&gt; for IRepository&lt;T&gt;.
/// </summary>
/// <example>
/// <code>
/// [OpenGenericRegistration(ServiceLifetime.Scoped)]
/// public class Repository&lt;T&gt; : IRepository&lt;T&gt; where T : class { }
///
/// [OpenGenericRegistration(ServiceLifetime.Singleton, ServiceType = typeof(ICache&lt;&gt;))]
/// public class RedisCache&lt;T&gt; : ICache&lt;T&gt; { }
/// </code>
/// </example>
/// <remarks>
/// Open generic registration allows a single registration to handle all closed generic types.
/// For example, registering Repository&lt;T&gt; for IRepository&lt;T&gt; will automatically resolve
/// IRepository&lt;User&gt;, IRepository&lt;Order&gt;, etc.
/// <para>
/// The service type is determined by convention (I{ClassName} without the generic arity suffix)
/// or can be explicitly specified using the <see cref="ServiceType"/> property.
/// </para>
/// </remarks>
/// <param name="lifetime">The service lifetime (Singleton, Scoped, or Transient)</param>
/// <seealso cref="ServiceRegistrationAttribute"/>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class OpenGenericRegistrationAttribute(ServiceLifetime lifetime) : Attribute
{
    /// <summary>
    /// Gets the service lifetime for dependency injection registration.
    /// </summary>
    public ServiceLifetime Lifetime { get; } = lifetime;

    /// <summary>
    /// Gets or sets the explicit open generic service type to register.
    /// If not specified, the service type is determined by convention
    /// (the first implemented open generic interface matching I{ClassName}).
    /// </summary>
    /// <example>
    /// <code>
    /// [OpenGenericRegistration(ServiceLifetime.Scoped, ServiceType = typeof(IRepository&lt;&gt;))]
    /// public class EfRepository&lt;T&gt; : IRepository&lt;T&gt; where T : class { }
    /// </code>
    /// </example>
    public Type? ServiceType { get; set; }

    /// <summary>
    /// Gets or sets whether to use TryAdd pattern for this registration.
    /// When true, the service will only be registered if no existing registration exists.
    /// Default is false.
    /// </summary>
    public bool UseTryAdd { get; set; } = false;

    /// <summary>
    /// Gets or sets the registration order. Services with lower order values are registered first.
    /// Default is 0.
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// Gets or sets the profile for conditional registration (e.g., "Development", "Production").
    /// If specified, the service will only be registered when the matching profile is active.
    /// </summary>
    public string? Profile { get; set; }
}
