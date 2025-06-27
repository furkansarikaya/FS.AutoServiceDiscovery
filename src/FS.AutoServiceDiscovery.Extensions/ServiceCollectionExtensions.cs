using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Attributes;
using FS.AutoServiceDiscovery.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FS.AutoServiceDiscovery.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAutoServices(this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return AddAutoServices(services, null, assemblies);
    }
    
    public static IServiceCollection AddAutoServices(this IServiceCollection services,
        Action<AutoServiceOptions>? configureOptions = null,
        params Assembly[] assemblies)
    {
        var options = new AutoServiceOptions();
        configureOptions?.Invoke(options);

        // Eğer assembly verilmezse, calling assembly'yi kullan
        if (!assemblies.Any())
        {
            assemblies = new[] { Assembly.GetCallingAssembly() };
        }

        var servicesToRegister = new List<ServiceRegistrationInfo>();

        foreach (var assembly in assemblies)
        {
            // Assembly'deki tüm class'ları tara - reflection magic burada başlıyor
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<ServiceRegistrationAttribute>() != null);

            foreach (var implementationType in types)
            {
                var attribute = implementationType.GetCustomAttribute<ServiceRegistrationAttribute>()!;

                // Profile kontrolü - environment-based registration
                if (!ShouldRegisterForProfile(attribute, options.Profile))
                    continue;

                // Test ortamı kontrolü - test-specific filtering
                if (options.IsTestEnvironment && attribute.IgnoreInTests)
                    continue;

                // Conditional kontrolü - feature flag based registration
                if (!ShouldRegisterConditional(implementationType, options.Configuration))
                    continue;

                // Service interface'ini belirle - convention over configuration
                var serviceType = DetermineServiceType(implementationType, attribute);

                if (serviceType != null)
                {
                    servicesToRegister.Add(new ServiceRegistrationInfo
                    {
                        ServiceType = serviceType,
                        ImplementationType = implementationType,
                        Lifetime = attribute.Lifetime,
                        Order = attribute.Order
                    });
                }
            }
        }

        // Order'a göre sırala ve register et - dependency graph sıralaması
        foreach (var serviceInfo in servicesToRegister.OrderBy(s => s.Order))
        {
            services.Add(new ServiceDescriptor(
                serviceInfo.ServiceType,
                serviceInfo.ImplementationType,
                serviceInfo.Lifetime));

            if (options.EnableLogging)
            {
                Console.WriteLine($"Registered: {serviceInfo.ServiceType.Name} -> {serviceInfo.ImplementationType.Name} " +
                                  $"({serviceInfo.Lifetime}, Order: {serviceInfo.Order})");
            }
        }

        return services;
    }
    
    private static bool ShouldRegisterForProfile(ServiceRegistrationAttribute attribute, string? profile)
    {
        // Eğer profile belirtilmemişse veya attribute'da profile yoksa register et
        if (string.IsNullOrEmpty(profile) || string.IsNullOrEmpty(attribute.Profile))
            return true;

        // Profile eşleşiyorsa register et
        return string.Equals(attribute.Profile, profile, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldRegisterConditional(Type implementationType, IConfiguration? configuration)
    {
        if (configuration == null) return true;

        var conditionalAttributes = implementationType.GetCustomAttributes<ConditionalServiceAttribute>();

        // Eğer conditional attribute yoksa register et
        if (!conditionalAttributes.Any()) return true;

        // Tüm conditional'lar true olmalı (AND logic)
        foreach (var conditional in conditionalAttributes)
        {
            var configValue = configuration[conditional.ConfigurationKey];
            if (!string.Equals(configValue, conditional.ExpectedValue, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static Type? DetermineServiceType(Type implementationType, ServiceRegistrationAttribute attribute)
    {
        // Eğer attribute'da explicit service type belirtilmişse onu kullan
        if (attribute.ServiceType != null)
        {
            return attribute.ServiceType;
        }

        // Convention: I{ClassName} interface'ini ara (örn: ProductService -> IProductService)
        var interfaceName = $"I{implementationType.Name}";
        var serviceInterface = implementationType.GetInterfaces()
            .FirstOrDefault(i => i.Name == interfaceName);

        if (serviceInterface != null)
        {
            return serviceInterface;
        }

        // Eğer tek bir interface implement ediyorsa onu kullan
        var interfaces = implementationType.GetInterfaces()
            .Where(i => !i.Name.StartsWith("System.")) // System interface'lerini hariç tut
            .ToArray();

        return interfaces.Length == 1
            ? interfaces[0]
            :
            // Hiçbiri bulunamazsa class'ın kendisini register et (concrete type registration)
            implementationType;
    }
}