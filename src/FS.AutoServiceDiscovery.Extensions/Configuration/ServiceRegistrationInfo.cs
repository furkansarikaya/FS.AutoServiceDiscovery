using Microsoft.Extensions.DependencyInjection;

namespace FS.AutoServiceDiscovery.Extensions.Configuration;

/// <summary>
/// Service registration bilgilerini tutan internal helper class.
/// Application layer'da çünkü bu bir application-level utility.
/// </summary>
internal class ServiceRegistrationInfo
{
    public Type ServiceType { get; set; } = null!;
    public Type ImplementationType { get; set; } = null!;
    public ServiceLifetime Lifetime { get; set; }
    public int Order { get; set; }
}