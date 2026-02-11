using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Attributes;
using FS.AutoServiceDiscovery.Extensions.Configuration;
using FS.AutoServiceDiscovery.Extensions.Diagnostics;
using FS.AutoServiceDiscovery.Extensions.Performance;
using FS.AutoServiceDiscovery.Extensions.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FS.AutoServiceDiscovery.Extensions;

/// <summary>
/// Enhanced extension methods for IServiceCollection that support both traditional and expression-based
/// conditional service registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Automatically discovers and registers services marked with ServiceRegistrationAttribute
    /// from the specified assemblies using default options.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="assemblies">The assemblies to scan for services. If none provided, uses the calling assembly</param>
    /// <returns>The service collection for method chaining</returns>
    [RequiresUnreferencedCode("Auto service discovery uses reflection to scan assemblies and types.")]
    public static IServiceCollection AddAutoServices(this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return AddAutoServices(services, null, assemblies);
    }

    /// <summary>
    /// Enhanced service discovery method that supports the new expression-based conditional system
    /// while maintaining full backward compatibility with existing string-based conditions.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="configureOptions">Optional configuration action for customizing discovery behavior</param>
    /// <param name="assemblies">The assemblies to scan for services</param>
    /// <returns>The service collection for method chaining</returns>
    [RequiresUnreferencedCode("Auto service discovery uses reflection to scan assemblies and types.")]
    public static IServiceCollection AddAutoServices(this IServiceCollection services,
        Action<AutoServiceOptions>? configureOptions = null,
        params Assembly[] assemblies)
    {
        var options = new AutoServiceOptions();
        configureOptions?.Invoke(options);

        if (options.EnablePerformanceOptimizations)
        {
            return services.AddAutoServicesWithPerformanceOptimizations(configureOptions, assemblies);
        }

        return AddAutoServicesEnhanced(services, options, assemblies);
    }

    /// <summary>
    /// Enhanced implementation of service discovery that integrates the new expression-based
    /// conditional system while preserving all existing functionality.
    /// </summary>
    [RequiresUnreferencedCode("Auto service discovery uses reflection to scan assemblies and types.")]
    private static IServiceCollection AddAutoServicesEnhanced(IServiceCollection services, AutoServiceOptions options, Assembly[] assemblies)
    {
        using var activity = AutoServiceDiscoveryMetrics.ActivitySource.StartActivity("ServiceDiscovery");
        var stopwatch = Stopwatch.StartNew();

        var logger = ResolveLogger(services);

        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetCallingAssembly()];
        }

        activity?.SetTag("assembly.count", assemblies.Length);

        var servicesToRegister = new List<ServiceRegistrationInfo>(capacity: 64);
        var decoratorsToApply = new List<(Type DecoratorType, Type ServiceType, int Order)>();

        var conditionalContext = CreateConditionalContext(options);

        foreach (var assembly in assemblies)
        {
            AutoServiceDiscoveryMetrics.AssembliesScanned.Add(1);
            var assemblyStopwatch = Stopwatch.StartNew();

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                logger.LogWarning("Assembly '{AssemblyName}' has type loading issues. Some types may be skipped.",
                    assembly.GetName().Name);
                types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
            }

            var candidateTypes = types.Where(t => t.IsClass && !t.IsAbstract);

            foreach (var implementationType in candidateTypes)
            {
                // Check for decorator attribute
                var decoratorAttr = implementationType.GetCustomAttribute<DecoratorServiceAttribute>();
                if (decoratorAttr != null)
                {
                    if (!ShouldRegisterForProfile(decoratorAttr.Profile, options.Profile))
                        continue;

                    // Validate decorator implements the target interface
                    if (!decoratorAttr.DecoratedServiceType.IsAssignableFrom(implementationType))
                    {
                        logger.LogWarning(
                            "Decorator '{DecoratorType}' does not implement decorated service type '{ServiceType}'. Skipping.",
                            implementationType.Name, decoratorAttr.DecoratedServiceType.Name);
                        AutoServiceDiscoveryMetrics.ServicesSkipped.Add(1,
                            new KeyValuePair<string, object?>("reason", "invalid_decorator"));
                        continue;
                    }

                    decoratorsToApply.Add((implementationType, decoratorAttr.DecoratedServiceType, decoratorAttr.Order));
                    continue;
                }

                // Check for open generic attribute
                var openGenericAttr = implementationType.GetCustomAttribute<OpenGenericRegistrationAttribute>();
                if (openGenericAttr != null && implementationType.IsGenericTypeDefinition)
                {
                    if (!ShouldRegisterForProfile(openGenericAttr.Profile, options.Profile))
                        continue;

                    var serviceType = DetermineOpenGenericServiceType(implementationType, openGenericAttr);
                    if (serviceType != null)
                    {
                        servicesToRegister.Add(new ServiceRegistrationInfo
                        {
                            ServiceType = serviceType,
                            ImplementationType = implementationType,
                            Lifetime = openGenericAttr.Lifetime,
                            Order = openGenericAttr.Order,
                            Profile = openGenericAttr.Profile,
                            UseTryAdd = openGenericAttr.UseTryAdd
                        });
                    }
                    continue;
                }

                // Check for standard service registration attribute
                var attribute = implementationType.GetCustomAttribute<ServiceRegistrationAttribute>();
                if (attribute == null)
                    continue;

                if (!ShouldRegisterForProfile(attribute, options.Profile))
                {
                    AutoServiceDiscoveryMetrics.ServicesSkipped.Add(1,
                        new KeyValuePair<string, object?>("reason", "profile_mismatch"));
                    continue;
                }

                if (options.IsTestEnvironment && attribute.IgnoreInTests)
                {
                    AutoServiceDiscoveryMetrics.ServicesSkipped.Add(1,
                        new KeyValuePair<string, object?>("reason", "test_excluded"));
                    continue;
                }

                if (!ShouldRegisterConditionalEnhanced(implementationType, conditionalContext, options, logger))
                {
                    AutoServiceDiscoveryMetrics.ServicesSkipped.Add(1,
                        new KeyValuePair<string, object?>("reason", "conditional_failed"));
                    continue;
                }

                var useTryAdd = attribute.UseTryAdd || options.UseTryAddByDefault;

                // Multiple interface registration
                if (attribute.ServiceTypes is { Length: > 0 })
                {
                    foreach (var svcType in attribute.ServiceTypes)
                    {
                        servicesToRegister.Add(new ServiceRegistrationInfo
                        {
                            ServiceType = svcType,
                            ImplementationType = implementationType,
                            Lifetime = attribute.Lifetime,
                            Order = attribute.Order,
                            Profile = attribute.Profile,
                            IgnoreInTests = attribute.IgnoreInTests,
                            ConditionalAttributes = implementationType.GetCustomAttributes<ConditionalServiceAttribute>().ToArray(),
                            ServiceKey = attribute.ServiceKey,
                            UseTryAdd = useTryAdd
                        });
                    }
                }
                else
                {
                    var serviceType = DetermineServiceType(implementationType, attribute);
                    if (serviceType != null)
                    {
                        servicesToRegister.Add(new ServiceRegistrationInfo
                        {
                            ServiceType = serviceType,
                            ImplementationType = implementationType,
                            Lifetime = attribute.Lifetime,
                            Order = attribute.Order,
                            Profile = attribute.Profile,
                            IgnoreInTests = attribute.IgnoreInTests,
                            ConditionalAttributes = implementationType.GetCustomAttributes<ConditionalServiceAttribute>().ToArray(),
                            ServiceKey = attribute.ServiceKey,
                            UseTryAdd = useTryAdd
                        });
                    }
                }
            }

            assemblyStopwatch.Stop();
            AutoServiceDiscoveryMetrics.AssemblyScanDuration.Record(assemblyStopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("assembly", assembly.GetName().Name ?? "unknown"));
        }

        // Register services ordered by priority
        foreach (var serviceInfo in servicesToRegister.OrderBy(s => s.Order))
        {
            RegisterService(services, serviceInfo, logger);
        }

        // Apply decorators after all services are registered (ordered by Order)
        foreach (var (decoratorType, serviceType, _) in decoratorsToApply.OrderBy(d => d.Order))
        {
            RegisterDecorator(services, serviceType, decoratorType, logger);
            AutoServiceDiscoveryMetrics.DecoratorsApplied.Add(1);

            logger.LogDebug("Decorated: {ServiceType} with {DecoratorType}", serviceType.Name, decoratorType.Name);
        }

        // Scope validation
        if (options.EnableScopeValidation)
        {
            var validationStopwatch = Stopwatch.StartNew();
            var validationResult = ScopeValidator.ValidateScopes(services);
            validationStopwatch.Stop();

            AutoServiceDiscoveryMetrics.ScopeValidationDuration.Record(validationStopwatch.Elapsed.TotalMilliseconds);
            AutoServiceDiscoveryMetrics.ScopeViolations.Add(validationResult.Violations.Count);
            AutoServiceDiscoveryMetrics.ScopeWarnings.Add(validationResult.Warnings.Count);

            foreach (var warning in validationResult.Warnings)
            {
                logger.LogWarning("Scope Warning: {Message}", warning.Message);
            }

            foreach (var violation in validationResult.Violations)
            {
                logger.LogError("Scope Violation: {Message}", violation.Message);
            }

            if (!validationResult.IsValid && options.ThrowOnScopeViolation)
            {
                var messages = string.Join(Environment.NewLine, validationResult.Violations.Select(v => v.Message));
                throw new InvalidOperationException(
                    $"Scope validation failed with {validationResult.Violations.Count} violation(s):{Environment.NewLine}{messages}");
            }
        }

        stopwatch.Stop();
        AutoServiceDiscoveryMetrics.DiscoveryDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
        activity?.SetTag("services.registered", servicesToRegister.Count);

        logger.LogInformation(
            "Service discovery completed in {Duration:F1}ms. Registered {Count} services from {AssemblyCount} assemblies.",
            stopwatch.Elapsed.TotalMilliseconds, servicesToRegister.Count, assemblies.Length);

        return services;
    }

    /// <summary>
    /// Registers a single service with support for keyed services and TryAdd pattern.
    /// </summary>
    private static void RegisterService(IServiceCollection services, ServiceRegistrationInfo serviceInfo, ILogger logger)
    {
        ServiceDescriptor descriptor;

        if (serviceInfo.ServiceKey != null)
        {
            descriptor = new ServiceDescriptor(
                serviceInfo.ServiceType,
                serviceInfo.ServiceKey,
                serviceInfo.ImplementationType,
                serviceInfo.Lifetime);
            AutoServiceDiscoveryMetrics.KeyedRegistrations.Add(1);
        }
        else
        {
            descriptor = new ServiceDescriptor(
                serviceInfo.ServiceType,
                serviceInfo.ImplementationType,
                serviceInfo.Lifetime);
        }

        if (serviceInfo.UseTryAdd)
        {
            var countBefore = services.Count;
            services.TryAdd(descriptor);
            if (services.Count == countBefore)
            {
                AutoServiceDiscoveryMetrics.DuplicatesPrevented.Add(1);
                logger.LogDebug("TryAdd skipped duplicate: {ServiceType}", serviceInfo.ServiceType.Name);
                return;
            }
        }
        else
        {
            services.Add(descriptor);
        }

        AutoServiceDiscoveryMetrics.ServicesRegistered.Add(1,
            new KeyValuePair<string, object?>("lifetime", serviceInfo.Lifetime.ToString()));

        logger.LogDebug("Registered: {ServiceType} -> {ImplementationType} ({Lifetime}, Order: {Order})",
            serviceInfo.ServiceType.Name, serviceInfo.ImplementationType.Name,
            serviceInfo.Lifetime, serviceInfo.Order);
    }

    /// <summary>
    /// Registers a decorator that wraps an existing service registration.
    /// The decorator must implement the same interface and accept it via constructor injection.
    /// </summary>
    private static void RegisterDecorator(IServiceCollection services, Type serviceType, Type decoratorType, ILogger logger)
    {
        var existingDescriptor = services.LastOrDefault(d => d.ServiceType == serviceType);
        if (existingDescriptor == null)
        {
            logger.LogWarning("Cannot apply decorator '{DecoratorType}': no existing registration for '{ServiceType}'.",
                decoratorType.Name, serviceType.Name);
            return;
        }

        services.Remove(existingDescriptor);

        services.Add(new ServiceDescriptor(
            serviceType,
            provider =>
            {
                var innerInstance = CreateInstance(provider, existingDescriptor);
                return ActivatorUtilities.CreateInstance(provider, decoratorType, innerInstance);
            },
            existingDescriptor.Lifetime));
    }

    /// <summary>
    /// Creates a service instance from an existing descriptor (used for decorator chaining).
    /// </summary>
    private static object CreateInstance(IServiceProvider provider, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance != null)
            return descriptor.ImplementationInstance;

        if (descriptor.ImplementationFactory != null)
            return descriptor.ImplementationFactory(provider);

        if (descriptor.ImplementationType != null)
            return ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType);

        throw new InvalidOperationException($"Cannot create instance for service type '{descriptor.ServiceType.Name}'.");
    }

    /// <summary>
    /// Determines the open generic service type for an open generic implementation.
    /// </summary>
    private static Type? DetermineOpenGenericServiceType(Type implementationType, OpenGenericRegistrationAttribute attribute)
    {
        if (attribute.ServiceType != null)
            return attribute.ServiceType;

        var baseName = implementationType.Name;
        var backtickIndex = baseName.IndexOf('`');
        if (backtickIndex > 0)
            baseName = baseName[..backtickIndex];

        var interfaceName = $"I{baseName}";

        var serviceInterface = implementationType.GetInterfaces()
            .FirstOrDefault(i =>
            {
                var iName = i.Name;
                var iBacktick = iName.IndexOf('`');
                if (iBacktick > 0) iName = iName[..iBacktick];
                return string.Equals(iName, interfaceName, StringComparison.Ordinal);
            });

        if (serviceInterface != null)
        {
            return serviceInterface.IsGenericType ? serviceInterface.GetGenericTypeDefinition() : serviceInterface;
        }

        var genericInterfaces = implementationType.GetInterfaces()
            .Where(i => i.IsGenericType)
            .ToArray();

        if (genericInterfaces.Length == 1)
            return genericInterfaces[0].GetGenericTypeDefinition();

        return null;
    }

    /// <summary>
    /// Creates a conditional context for expression-based conditions.
    /// </summary>
    private static IConditionalContext CreateConditionalContext(AutoServiceOptions options)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                             ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                             ?? (options.IsTestEnvironment ? "Testing" : "Production");

        return new ConditionalContext(options.Configuration, environmentName);
    }

    /// <summary>
    /// Enhanced conditional evaluation that supports both traditional string-based conditions
    /// and the new expression-based conditional system.
    /// </summary>
    private static bool ShouldRegisterConditionalEnhanced(Type implementationType, IConditionalContext context,
        AutoServiceOptions options, ILogger logger)
    {
        var conditionalAttributes = implementationType.GetCustomAttributes<ConditionalServiceAttribute>().ToArray();

        if (conditionalAttributes.Length == 0)
            return true;

        foreach (var conditional in conditionalAttributes)
        {
            try
            {
                if (!conditional.EvaluateCondition(context))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to evaluate conditional for {TypeName}. Treating as not registered.",
                    implementationType.Name);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Profile filtering logic for ServiceRegistrationAttribute.
    /// </summary>
    private static bool ShouldRegisterForProfile(ServiceRegistrationAttribute attribute, string? profile)
    {
        return ShouldRegisterForProfile(attribute.Profile, profile);
    }

    /// <summary>
    /// Profile filtering logic that checks whether a service's profile matches the active profile.
    /// </summary>
    private static bool ShouldRegisterForProfile(string? attributeProfile, string? activeProfile)
    {
        if (string.IsNullOrEmpty(activeProfile) || string.IsNullOrEmpty(attributeProfile))
            return true;

        return string.Equals(attributeProfile, activeProfile, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Service type determination using convention-based discovery.
    /// </summary>
    private static Type? DetermineServiceType(Type implementationType, ServiceRegistrationAttribute attribute)
    {
        if (attribute.ServiceType != null)
        {
            return attribute.ServiceType;
        }

        var interfaceName = $"I{implementationType.Name}";
        var serviceInterface = implementationType.GetInterfaces()
            .FirstOrDefault(i => string.Equals(i.Name, interfaceName, StringComparison.Ordinal));

        if (serviceInterface != null)
        {
            return serviceInterface;
        }

        var interfaces = implementationType.GetInterfaces()
            .Where(i => !i.Namespace?.StartsWith("System", StringComparison.Ordinal) == true)
            .ToArray();

        if (interfaces.Length == 1)
        {
            return interfaces[0];
        }

        return implementationType;
    }

    /// <summary>
    /// Resolves an ILogger from the service collection, or returns a NullLogger.
    /// </summary>
    private static ILogger ResolveLogger(IServiceCollection services)
    {
        var loggerFactoryDescriptor = services.LastOrDefault(d => d.ServiceType == typeof(ILoggerFactory));
        if (loggerFactoryDescriptor?.ImplementationInstance is ILoggerFactory factory)
        {
            return factory.CreateLogger("FS.AutoServiceDiscovery");
        }

        return NullLogger.Instance;
    }
}
