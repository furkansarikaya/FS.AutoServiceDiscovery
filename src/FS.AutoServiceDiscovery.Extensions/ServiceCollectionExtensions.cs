using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Attributes;
using FS.AutoServiceDiscovery.Extensions.Configuration;
using FS.AutoServiceDiscovery.Extensions.Performance;
using FS.AutoServiceDiscovery.Extensions.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FS.AutoServiceDiscovery.Extensions;

/// <summary>
/// Enhanced extension methods for IServiceCollection that support both traditional and expression-based
/// conditional service registration.
/// 
/// This enhanced implementation demonstrates how sophisticated systems can evolve while maintaining
/// backward compatibility. Like adding smart features to a car while keeping the basic driving
/// experience familiar, we're adding powerful expression capabilities while ensuring existing
/// code continues to work without modification.
/// 
/// The key insight here is that we're not replacing the old system - we're extending it. This
/// approach allows teams to migrate gradually, adopting the new expression-based system at their
/// own pace while maintaining their existing investments in string-based conditions.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Automatically discovers and registers services marked with ServiceRegistrationAttribute 
    /// from the specified assemblies using default options.
    /// 
    /// This method serves as the entry point for the enhanced conditional registration system.
    /// Under the hood, it now supports both traditional string-based conditions and the new
    /// expression-based DSL, making the transition seamless for developers.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="assemblies">The assemblies to scan for services. If none provided, uses the calling assembly</param>
    /// <returns>The service collection for method chaining</returns>
    /// <example>
    /// Simple usage that works with both old and new conditional styles:
    /// <code>
    /// services.AddAutoServices(Assembly.GetExecutingAssembly());
    /// </code>
    /// </example>
    public static IServiceCollection AddAutoServices(this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return AddAutoServices(services, null, assemblies);
    }
    
    /// <summary>
    /// Enhanced service discovery method that supports the new expression-based conditional system
    /// while maintaining full backward compatibility with existing string-based conditions.
    /// 
    /// This method represents the evolution of our service discovery system. Think of it like
    /// upgrading from a basic calculator to a scientific calculator - the basic math operations
    /// still work exactly the same way, but now you have access to much more sophisticated
    /// mathematical functions when you need them.
    /// </summary>
    /// <param name="services">The service collection to add services to</param>
    /// <param name="configureOptions">Optional configuration action for customizing discovery behavior</param>
    /// <param name="assemblies">The assemblies to scan for services</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddAutoServices(this IServiceCollection services,
        Action<AutoServiceOptions>? configureOptions = null,
        params Assembly[] assemblies)
    {
        var options = new AutoServiceOptions();
        configureOptions?.Invoke(options);

        // Performance optimization: if performance enhancements are enabled, delegate to the optimized version
        // This demonstrates how we can layer optimizations on top of our enhanced functionality
        if (options.EnablePerformanceOptimizations)
        {
            return services.AddAutoServicesWithPerformanceOptimizations(configureOptions, assemblies);
        }

        // Continue with our enhanced legacy implementation that now supports expression-based conditions
        return AddAutoServicesEnhanced(services, options, assemblies);
    }
    
    /// <summary>
    /// Enhanced implementation of service discovery that integrates the new expression-based
    /// conditional system while preserving all existing functionality.
    /// 
    /// This method demonstrates a thoughtful approach to system evolution. Rather than breaking
    /// existing functionality, we've enhanced the evaluation engine to understand both old and
    /// new conditional formats. This allows teams to migrate at their own pace while immediately
    /// benefiting from improved capabilities.
    /// </summary>
    private static IServiceCollection AddAutoServicesEnhanced(IServiceCollection services, AutoServiceOptions options, Assembly[] assemblies)
    {
        // Default to calling assembly if none specified - maintains existing behavior
        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetCallingAssembly()];
        }

        var servicesToRegister = new List<ServiceRegistrationInfo>();
        var decoratorsToApply = new List<(Type DecoratorType, Type ServiceType, int Order)>();

        // Create the enhanced conditional context once for all evaluations
        var conditionalContext = CreateConditionalContext(options);

        foreach (var assembly in assemblies)
        {
            // Scan for standard service candidates
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract);

            foreach (var implementationType in types)
            {
                // Check for decorator attribute
                var decoratorAttr = implementationType.GetCustomAttribute<DecoratorServiceAttribute>();
                if (decoratorAttr != null)
                {
                    if (!ShouldRegisterForProfile(decoratorAttr.Profile, options.Profile))
                        continue;
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

                // Profile filtering
                if (!ShouldRegisterForProfile(attribute, options.Profile))
                    continue;

                // Test environment filtering
                if (options.IsTestEnvironment && attribute.IgnoreInTests)
                    continue;

                // Enhanced conditional filtering
                if (!ShouldRegisterConditionalEnhanced(implementationType, conditionalContext, options))
                    continue;

                // Determine UseTryAdd: attribute-level overrides global default
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
                    // Single service type (standard behavior)
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
        }

        // Register services ordered by priority
        foreach (var serviceInfo in servicesToRegister.OrderBy(s => s.Order))
        {
            RegisterService(services, serviceInfo, options);
        }

        // Apply decorators after all services are registered (ordered by Order)
        foreach (var (decoratorType, serviceType, _) in decoratorsToApply.OrderBy(d => d.Order))
        {
            RegisterDecorator(services, serviceType, decoratorType);

            if (options.EnableLogging)
            {
                Console.WriteLine($"Decorated: {serviceType.Name} with {decoratorType.Name}");
            }
        }

        // Scope validation
        if (options.EnableScopeValidation)
        {
            var validationResult = ScopeValidator.ValidateScopes(services);

            if (options.EnableLogging)
            {
                foreach (var warning in validationResult.Warnings)
                {
                    Console.WriteLine($"Scope Warning: {warning.Message}");
                }

                foreach (var violation in validationResult.Violations)
                {
                    Console.WriteLine($"Scope Violation: {violation.Message}");
                }
            }

            if (!validationResult.IsValid && options.ThrowOnScopeViolation)
            {
                var messages = string.Join(Environment.NewLine, validationResult.Violations.Select(v => v.Message));
                throw new InvalidOperationException(
                    $"Scope validation failed with {validationResult.Violations.Count} violation(s):{Environment.NewLine}{messages}");
            }
        }

        return services;
    }

    /// <summary>
    /// Registers a single service with support for keyed services and TryAdd pattern.
    /// </summary>
    private static void RegisterService(IServiceCollection services, ServiceRegistrationInfo serviceInfo, AutoServiceOptions options)
    {
        ServiceDescriptor descriptor;

        if (serviceInfo.ServiceKey != null)
        {
            descriptor = new ServiceDescriptor(
                serviceInfo.ServiceType,
                serviceInfo.ServiceKey,
                serviceInfo.ImplementationType,
                serviceInfo.Lifetime);
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
            services.TryAdd(descriptor);
        }
        else
        {
            services.Add(descriptor);
        }

        if (options.EnableLogging)
        {
            var keyInfo = serviceInfo.ServiceKey != null ? $", Key: {serviceInfo.ServiceKey}" : "";
            var tryAddInfo = serviceInfo.UseTryAdd ? ", TryAdd" : "";
            Console.WriteLine($"Registered: {serviceInfo.ServiceType.Name} -> {serviceInfo.ImplementationType.Name} " +
                              $"({serviceInfo.Lifetime}, Order: {serviceInfo.Order}{keyInfo}{tryAddInfo})");
        }
    }

    /// <summary>
    /// Registers a decorator that wraps an existing service registration.
    /// The decorator must implement the same interface and accept it via constructor injection.
    /// </summary>
    private static void RegisterDecorator(IServiceCollection services, Type serviceType, Type decoratorType)
    {
        var existingDescriptor = services.LastOrDefault(d => d.ServiceType == serviceType);
        if (existingDescriptor == null) return;

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

        // Convention: look for an open generic interface matching I{ClassName} pattern
        // For Repository`1, look for IRepository`1
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
                return iName == interfaceName;
            });

        if (serviceInterface != null)
        {
            return serviceInterface.IsGenericType ? serviceInterface.GetGenericTypeDefinition() : serviceInterface;
        }

        // Fallback: use first open generic interface
        var genericInterfaces = implementationType.GetInterfaces()
            .Where(i => i.IsGenericType)
            .ToArray();

        if (genericInterfaces.Length == 1)
            return genericInterfaces[0].GetGenericTypeDefinition();

        return null;
    }

    /// <summary>
    /// Creates a rich conditional context that provides the foundation for expression-based conditions.
    /// 
    /// Think of this method as setting up a "control room" that your conditional expressions can use
    /// to gather information about the current environment, configuration, and application state.
    /// The context provides a standardized way for expressions to access all the information they
    /// need to make intelligent registration decisions.
    /// 
    /// This factory method demonstrates good separation of concerns - it isolates the context
    /// creation logic so that it can be easily tested, modified, or extended without affecting
    /// the main discovery logic.
    /// </summary>
    /// <param name="options">The current auto service options containing configuration and environment info</param>
    /// <returns>A fully configured conditional context ready for expression evaluation</returns>
    private static IConditionalContext CreateConditionalContext(AutoServiceOptions options)
    {
        // Determine the current environment name from various sources
        // This logic shows how we can intelligently detect the environment using multiple fallback strategies
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                             ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                             ?? (options.IsTestEnvironment ? "Testing" : "Production");

        // Create the context with all available information
        // The context becomes the "bridge" between the static attribute declarations and the dynamic runtime environment
        var context = new ConditionalContext(options.Configuration, environmentName);

        // Here's where you could extend the context with application-specific custom conditions
        // For example, you might register conditions that check database connectivity,
        // external service availability, or complex business rules
        RegisterCustomConditions(context, options);

        return context;
    }

    /// <summary>
    /// Registers application-specific custom conditions that can be used in expressions.
    /// 
    /// This method demonstrates the extensibility of our conditional system. Just like a programming
    /// language allows you to define your own functions, our conditional system allows applications
    /// to define their own named conditions that can be used in expressions.
    /// 
    /// Think of custom conditions as "domain-specific vocabulary" for your conditional expressions.
    /// Instead of writing complex logic in every expression, you can define named conditions once
    /// and reuse them throughout your application.
    /// </summary>
    /// <param name="context">The conditional context to register custom conditions with</param>
    /// <param name="options">The auto service options that might influence custom condition behavior</param>
    private static void RegisterCustomConditions(ConditionalContext context, AutoServiceOptions options)
    {
        // Example: Register a custom condition for checking if the application is in maintenance mode
        // This shows how complex business logic can be encapsulated in named conditions
        context.RegisterCustomCondition("MaintenanceMode", () =>
        {
            // This could check multiple sources: configuration, database, external service, etc.
            var configMaintenanceMode = options.Configuration?.GetValue<bool>("MaintenanceMode") ?? false;
            var environmentMaintenanceMode = Environment.GetEnvironmentVariable("MAINTENANCE_MODE") == "true";
            
            return configMaintenanceMode || environmentMaintenanceMode;
        });

        // Example: Register a condition for checking if database connectivity is available
        // This demonstrates how infrastructure concerns can be made available to service registration logic
        context.RegisterCustomCondition("DatabaseAvailable", () =>
        {
            // In a real application, this might ping the database or check connection strings
            // For this example, we'll use a simple configuration check
            var connectionString = options.Configuration?.GetConnectionString("DefaultConnection");
            return !string.IsNullOrEmpty(connectionString);
        });

        // Example: Register a condition for checking if external dependencies are healthy
        // This shows how service registration can be made aware of external system availability
        context.RegisterCustomCondition("ExternalServicesHealthy", () =>
        {
            // This could perform actual health checks or consult a service discovery system
            // For demonstration, we'll use configuration-based health indicators
            return options.Configuration?.GetValue<bool>("ExternalServices:AllHealthy") ?? true;
        });
    }

    /// <summary>
    /// Enhanced conditional evaluation that supports both traditional string-based conditions
    /// and the new expression-based conditional system.
    /// 
    /// This method represents the heart of our backward-compatible enhancement strategy. Like a
    /// universal translator that can understand both old and new languages, this method can
    /// evaluate conditions regardless of which format they're written in.
    /// 
    /// The key insight here is that we're not asking users to choose between old and new systems.
    /// Instead, we're allowing them to use whichever approach is most appropriate for each specific
    /// condition, and even mix and match approaches within the same application.
    /// </summary>
    /// <param name="implementationType">The type being evaluated for conditional registration</param>
    /// <param name="context">The rich conditional context for expression evaluation</param>
    /// <param name="options">The auto service options for backward compatibility</param>
    /// <returns>True if all conditional requirements are satisfied and the service should be registered</returns>
    private static bool ShouldRegisterConditionalEnhanced(Type implementationType, IConditionalContext context, AutoServiceOptions options)
    {
        // Get all conditional attributes from the type
        // A service can have multiple conditional attributes, and ALL must be satisfied (AND logic)
        var conditionalAttributes = implementationType.GetCustomAttributes<ConditionalServiceAttribute>().ToArray();

        // If no conditional attributes exist, the service should be registered unconditionally
        // This maintains the existing behavior for services without conditional requirements
        if (conditionalAttributes.Length == 0)
            return true;

        // Evaluate each conditional attribute using the enhanced evaluation system
        // This loop demonstrates how we handle multiple conditions with AND logic
        foreach (var conditional in conditionalAttributes)
        {
            try
            {
                // Use the enhanced attribute's evaluation method that automatically handles
                // both string-based and expression-based conditions
                if (!conditional.EvaluateCondition(context))
                {
                    // If any condition fails, the entire evaluation fails (AND logic)
                    // This maintains the existing behavior while supporting new expression formats
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Graceful error handling: if a condition evaluation fails, log the error
                // and treat the condition as failed rather than crashing the entire discovery process
                if (options.EnableLogging)
                {
                    Console.WriteLine($"Warning: Failed to evaluate conditional for {implementationType.Name}: {ex.Message}");
                }
                
                // Failed condition evaluation results in service not being registered
                // This conservative approach ensures that registration errors don't cause runtime failures
                return false;
            }
        }

        // If we reach this point, all conditions passed successfully
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
    /// This method remains unchanged, demonstrating how solid foundational logic can be preserved
    /// while other parts of the system evolve and improve.
    /// </summary>
    private static Type? DetermineServiceType(Type implementationType, ServiceRegistrationAttribute attribute)
    {
        // Explicit service type takes precedence over convention-based discovery
        if (attribute.ServiceType != null)
        {
            return attribute.ServiceType;
        }

        // Convention-based discovery: look for I{ClassName} interface pattern
        // This implements the widely-adopted .NET naming convention
        var interfaceName = $"I{implementationType.Name}";
        var serviceInterface = implementationType.GetInterfaces()
            .FirstOrDefault(i => i.Name == interfaceName);

        if (serviceInterface != null)
        {
            return serviceInterface;
        }

        // Fallback: if only one non-system interface exists, use it
        var interfaces = implementationType.GetInterfaces()
            .Where(i => !i.Name.StartsWith("System."))
            .ToArray();

        if (interfaces.Length == 1)
        {
            return interfaces[0];
        }

        // Final fallback: register the concrete type itself
        // This enables scenarios where interface-based registration isn't desired
        return implementationType;
    }
}