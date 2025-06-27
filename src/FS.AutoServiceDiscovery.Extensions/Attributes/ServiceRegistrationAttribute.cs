using Microsoft.Extensions.DependencyInjection;

namespace FS.AutoServiceDiscovery.Extensions.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ServiceRegistrationAttribute(ServiceLifetime lifetime) : Attribute
{
    public ServiceLifetime Lifetime { get; } = lifetime;
    public int Order { get; set; } = 0;
    public Type? ServiceType { get; set; } // Eğer farklı bir interface implement etmek istiyorsak
    public string? Profile { get; set; } // Development, Production gibi profile'lar için
    public bool IgnoreInTests { get; set; } = false; // Test ortamında ignore et
}