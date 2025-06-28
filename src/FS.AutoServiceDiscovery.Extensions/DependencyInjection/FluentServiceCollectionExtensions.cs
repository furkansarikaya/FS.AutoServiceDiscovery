using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Architecture;
using FS.AutoServiceDiscovery.Extensions.Architecture.Conventions;
using FS.AutoServiceDiscovery.Extensions.Configuration;
using FS.AutoServiceDiscovery.Extensions.Configuration.FluentConfiguration;
using FS.AutoServiceDiscovery.Extensions.Performance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FS.AutoServiceDiscovery.Extensions.DependencyInjection;

/// <summary>
/// Provides fluent extension methods for IServiceCollection that enable sophisticated service discovery configuration
/// using method chaining and natural language-like syntax.
/// 
/// This class represents the bridge between the fluent configuration API and the traditional service collection
/// registration process. Think of these extension methods as translators that take the sophisticated fluent
/// configuration language and convert it into the actual service registrations that the dependency injection
/// container can understand and execute.
/// 
/// The fluent approach implemented here transforms service discovery from a simple, single-method call into
/// a rich configuration experience that can express complex requirements, conditions, and customizations in
/// a way that reads almost like documentation. Instead of cramming all configuration into method parameters
/// or options objects, the fluent API allows developers to build up their configuration incrementally,
/// making the code both more readable and more maintainable.
/// 
/// These extension methods serve as the entry point for developers who want to use the advanced configuration
/// capabilities while still working within the familiar IServiceCollection pattern that .NET developers expect.
/// </summary>
public static class FluentServiceCollectionExtensions
{
    /// <summary>
    /// Starts a fluent configuration for auto service discovery, providing a sophisticated alternative
    /// to the traditional single-method service registration approach.
    /// 
    /// This method initiates a fluent configuration session that allows developers to build complex
    /// service discovery configurations using method chaining. Unlike traditional configuration approaches
    /// that require all settings to be specified upfront in a single method call, the fluent approach
    /// allows configuration to be built incrementally, with each method call adding another piece of
    /// the overall configuration puzzle.
    /// 
    /// The fluent pattern is particularly valuable for service discovery because it allows developers
    /// to express complex conditional logic, filtering rules, and customizations in a way that remains
    /// readable and maintainable even as the configuration grows in complexity. Each method in the chain
    /// adds clarity about what the configuration is trying to achieve, making the code self-documenting.
    /// 
    /// This method returns a configuration builder that provides access to all the advanced configuration
    /// options through a natural, discoverable API. IntelliSense support makes it easy to explore the
    /// available options and build sophisticated configurations without needing to consult documentation.
    /// </summary>
    /// <param name="services">
    /// The service collection that will receive the discovered services. This collection serves as
    /// the target for all service registrations that result from the discovery process.
    /// </param>
    /// <returns>
    /// A fluent configuration builder that enables method chaining for sophisticated service discovery
    /// configuration. The builder provides access to all advanced configuration options including
    /// assembly selection, type filtering, conditional registration, plugin integration, and more.
    /// </returns>
    /// <example>
    /// Basic usage with method chaining:
    /// <code>
    /// services.ConfigureAutoServices()
    ///     .FromAssemblies(Assembly.GetExecutingAssembly())
    ///     .WithProfile("Production")
    ///     .WithPerformanceOptimizations()
    ///     .Apply();
    /// </code>
    /// 
    /// Complex configuration with conditional logic:
    /// <code>
    /// services.ConfigureAutoServices()
    ///     .FromCurrentDomain(assembly => !assembly.FullName.StartsWith("System"))
    ///     .WithProfile(ctx => ctx.Environment == "Development" ? "Dev" : "Prod")
    ///     .When(ctx => ctx.Configuration["FeatureFlags:EnableAdvancedServices"] == "true")
    ///     .ExcludeNamespaces("MyApp.Internal.*", "MyApp.Testing.*")
    ///     .WithDefaultLifetime(ServiceLifetime.Scoped)
    ///     .WithLogging(true)
    ///     .Apply();
    /// </code>
    /// </example>
    public static FluentAutoServiceConfigurationBuilder ConfigureAutoServices(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        return new FluentAutoServiceConfigurationBuilder(services);
    }

    /// <summary>
    /// Provides a quick-start fluent configuration that automatically includes the calling assembly
    /// and applies sensible defaults for most common scenarios.
    /// 
    /// This method serves as a convenience entry point for developers who want to use the fluent
    /// configuration API but don't want to manually specify the assembly selection. It automatically
    /// includes the assembly that contains the code making this call, which covers the most common
    /// use case where developers want to register services from their current project.
    /// 
    /// The quick-start approach reduces the ceremony required to get started with fluent configuration
    /// while still providing access to all the advanced configuration options. Developers can start
    /// with this method and then chain additional configuration methods to customize the behavior
    /// for their specific needs.
    /// 
    /// This pattern follows the principle of "sensible defaults with easy customization" - the method
    /// makes reasonable assumptions about what most developers want while providing clear paths to
    /// customize the behavior when those assumptions don't match the specific requirements.
    /// </summary>
    /// <param name="services">
    /// The service collection that will receive the discovered services.
    /// </param>
    /// <returns>
    /// A fluent configuration builder pre-configured with the calling assembly and ready for
    /// additional customization through method chaining.
    /// </returns>
    /// <example>
    /// Quick start with minimal configuration:
    /// <code>
    /// services.ConfigureAutoServicesFromCurrentAssembly()
    ///     .WithProfile("Production")
    ///     .Apply();
    /// </code>
    /// 
    /// Quick start with additional customization:
    /// <code>
    /// services.ConfigureAutoServicesFromCurrentAssembly()
    ///     .WithConfiguration(configuration)
    ///     .When("FeatureFlags:EnableAutoDiscovery", "true")
    ///     .ExcludeTypes(type => type.Name.EndsWith("Test"))
    ///     .WithPerformanceOptimizations()
    ///     .Apply();
    /// </code>
    /// </example>
    public static FluentAutoServiceConfigurationBuilder ConfigureAutoServicesFromCurrentAssembly(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        var callingAssembly = Assembly.GetCallingAssembly();
        
        return new FluentAutoServiceConfigurationBuilder(services)
            .FromAssemblies(callingAssembly);
    }

    /// <summary>
    /// Creates a fluent configuration builder that starts with a pre-specified set of assemblies,
    /// providing immediate assembly selection while maintaining access to all advanced configuration options.
    /// 
    /// This method bridges the gap between the traditional approach of specifying assemblies upfront
    /// and the fluent approach of building configuration incrementally. It allows developers to specify
    /// their target assemblies immediately while still benefiting from the rich configuration options
    /// provided by the fluent API.
    /// 
    /// This approach is particularly useful when the assembly selection logic is straightforward but
    /// other aspects of the configuration (such as filtering, conditional registration, or performance
    /// tuning) require more sophisticated setup. By handling the assembly selection upfront, developers
    /// can focus the rest of their configuration chain on the more complex requirements.
    /// 
    /// The method maintains the fluent pattern by returning a configuration builder that already has
    /// the assembly selection applied, ready for additional configuration through method chaining.
    /// </summary>
    /// <param name="services">
    /// The service collection that will receive the discovered services.
    /// </param>
    /// <param name="assemblies">
    /// The assemblies to include in the service discovery process. These assemblies will be scanned
    /// for services marked with registration attributes.
    /// </param>
    /// <returns>
    /// A fluent configuration builder pre-configured with the specified assemblies and ready for
    /// additional customization through method chaining.
    /// </returns>
    /// <example>
    /// Configuration with explicit assembly selection:
    /// <code>
    /// services.ConfigureAutoServicesFromAssemblies(
    ///         Assembly.GetExecutingAssembly(),
    ///         Assembly.GetAssembly(typeof(SomeOtherClass)))
    ///     .WithProfile("Production")
    ///     .ExcludeNamespaces("Internal.*")
    ///     .WithDefaultLifetime(ServiceLifetime.Scoped)
    ///     .Apply();
    /// </code>
    /// 
    /// Configuration with plugin assemblies:
    /// <code>
    /// var pluginAssemblies = LoadPluginAssemblies();
    /// services.ConfigureAutoServicesFromAssemblies(pluginAssemblies)
    ///     .When(ctx => ctx.Configuration["EnablePlugins"] == "true")
    ///     .WithNamingConvention&lt;CustomNamingConvention&gt;()
    ///     .WithLogging(true)
    ///     .Apply();
    /// </code>
    /// </example>
    public static FluentAutoServiceConfigurationBuilder ConfigureAutoServicesFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

        return new FluentAutoServiceConfigurationBuilder(services)
            .FromAssemblies(assemblies);
    }
}

/// <summary>
/// Provides a fluent configuration builder that wraps the IFluentAutoServiceConfiguration interface
/// and adds the ability to apply the configuration to a specific service collection.
/// 
/// This class serves as an adapter that bridges the gap between the generic fluent configuration
/// interface and the specific service collection that needs to receive the discovered services.
/// Think of this class as a specialized wrapper that remembers which service collection is the
/// target for the configuration and provides a convenient way to apply the configuration when
/// the setup is complete.
/// 
/// The builder pattern implemented here allows for a clear separation between the configuration
/// phase (where developers specify what they want) and the execution phase (where the configuration
/// is actually applied to register services). This separation provides several benefits including
/// better testability, clearer error handling, and the ability to validate the configuration
/// before attempting to apply it.
/// 
/// By wrapping the core fluent configuration functionality, this builder maintains a clean
/// separation of concerns while providing the convenience methods that developers expect when
/// working with IServiceCollection extensions.
/// </summary>
public class FluentAutoServiceConfigurationBuilder
{
    private readonly IServiceCollection _services;
    private readonly IFluentAutoServiceConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the fluent configuration builder with the specified service collection.
    /// 
    /// The constructor establishes the connection between the fluent configuration system and the
    /// specific service collection that will receive the discovered services. This connection is
    /// maintained throughout the configuration process and used when the configuration is finally
    /// applied.
    /// </summary>
    /// <param name="services">
    /// The service collection that will be the target for service registrations when the
    /// configuration is applied.
    /// </param>
    internal FluentAutoServiceConfigurationBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _configuration = new FluentAutoServiceConfiguration();

        // Initialize the configuration context with the service collection
        _configuration.Context.ServiceCollection = services;
    }

    /// <summary>
    /// Specifies the assemblies to scan for service discovery.
    /// This method delegates to the underlying fluent configuration to establish the scope
    /// of the discovery operation.
    /// </summary>
    /// <param name="assemblies">
    /// The assemblies to include in the discovery process.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder FromAssemblies(params Assembly[] assemblies)
    {
        _configuration.FromAssemblies(assemblies);
        return this;
    }

    /// <summary>
    /// Specifies assemblies to scan by loading them from the given file paths.
    /// This method provides an alternative way to specify assemblies when you have file paths
    /// rather than Assembly objects.
    /// </summary>
    /// <param name="assemblyPaths">
    /// File paths to assembly files that should be loaded and scanned for services.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder FromAssemblyPaths(params string[] assemblyPaths)
    {
        _configuration.FromAssemblyPaths(assemblyPaths);
        return this;
    }

    /// <summary>
    /// Automatically discovers and includes assemblies from the current application domain.
    /// This method scans the current AppDomain for loaded assemblies and includes those
    /// that match the specified filter criteria.
    /// </summary>
    /// <param name="assemblyFilter">
    /// Optional filter to determine which assemblies from the AppDomain should be included.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder FromCurrentDomain(Func<Assembly, bool>? assemblyFilter = null)
    {
        _configuration.FromCurrentDomain(assemblyFilter);
        return this;
    }

    /// <summary>
    /// Sets the active profile for profile-based service registration.
    /// Services marked with specific profiles will only be registered when the active profile
    /// matches their profile configuration.
    /// </summary>
    /// <param name="profile">
    /// The profile name to activate.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder WithProfile(string profile)
    {
        _configuration.WithProfile(profile);
        return this;
    }

    /// <summary>
    /// Sets the active profile using a dynamic resolver function.
    /// This allows the profile to be determined at runtime based on configuration,
    /// environment variables, or other dynamic factors.
    /// </summary>
    /// <param name="profileResolver">
    /// A function that takes the current configuration context and returns the appropriate
    /// profile name.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder WithProfile(Func<ConfigurationContext, string> profileResolver)
    {
        _configuration.WithProfile(profileResolver);
        return this;
    }

    /// <summary>
    /// Adds a conditional constraint that must be satisfied for discovery to proceed.
    /// This method enables complex conditional logic for service registration.
    /// </summary>
    /// <param name="condition">
    /// A predicate function that receives the configuration context and returns true
    /// if the condition is satisfied.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder When(Func<ConfigurationContext, bool> condition)
    {
        _configuration.When(condition);
        return this;
    }

    /// <summary>
    /// Adds a conditional constraint based on a simple configuration value check.
    /// This is a convenience method for the common scenario of checking configuration values.
    /// </summary>
    /// <param name="configurationKey">The configuration key to check.</param>
    /// <param name="expectedValue">The expected value for the configuration key.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder When(string configurationKey, string expectedValue)
    {
        _configuration.When(configurationKey, expectedValue);
        return this;
    }

    /// <summary>
    /// Excludes types that match the specified filter from service discovery.
    /// This method provides fine-grained control over which types should be ignored
    /// during the discovery process.
    /// </summary>
    /// <param name="typeFilter">
    /// A predicate function that receives a type and returns true if the type should
    /// be excluded from discovery.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder ExcludeTypes(Func<Type, bool> typeFilter)
    {
        _configuration.ExcludeTypes(typeFilter);
        return this;
    }

    /// <summary>
    /// Includes only types that match the specified filter in service discovery.
    /// This method provides a whitelist approach to type filtering.
    /// </summary>
    /// <param name="typeFilter">
    /// A predicate function that receives a type and returns true if the type should
    /// be included in discovery.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder IncludeOnlyTypes(Func<Type, bool> typeFilter)
    {
        _configuration.IncludeOnlyTypes(typeFilter);
        return this;
    }

    /// <summary>
    /// Excludes types from specific namespaces from service discovery.
    /// This is a convenience method for namespace-based filtering.
    /// </summary>
    /// <param name="namespaces">
    /// The namespaces to exclude. Supports both exact matches and wildcard patterns.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder ExcludeNamespaces(params string[] namespaces)
    {
        _configuration.ExcludeNamespaces(namespaces);
        return this;
    }

    /// <summary>
    /// Sets a default service lifetime for services that don't specify one explicitly.
    /// This provides a fallback lifetime for services that don't have explicit lifetime
    /// configuration in their registration attributes.
    /// </summary>
    /// <param name="lifetime">
    /// The default service lifetime to use.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder WithDefaultLifetime(ServiceLifetime lifetime)
    {
        _configuration.WithDefaultLifetime(lifetime);
        return this;
    }

    /// <summary>
    /// Adds a custom naming convention for service type resolution.
    /// Naming conventions determine how implementation types are matched with service interfaces.
    /// </summary>
    /// <param name="convention">
    /// The naming convention instance to add to the resolution process.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder WithNamingConvention(INamingConvention convention)
    {
        _configuration.WithNamingConvention(convention);
        return this;
    }

    /// <summary>
    /// Adds a custom naming convention using a type that will be instantiated via dependency injection.
    /// This allows naming conventions to have their own dependencies injected during construction.
    /// </summary>
    /// <typeparam name="TNamingConvention">
    /// The type of naming convention to add.
    /// </typeparam>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder WithNamingConvention<TNamingConvention>()
        where TNamingConvention : class, INamingConvention
    {
        _configuration.WithNamingConvention<TNamingConvention>();
        return this;
    }

    /// <summary>
    /// Adds a service discovery plugin to extend the discovery process with custom logic.
    /// Plugins allow for specialized discovery strategies that go beyond the standard
    /// attribute-based approach.
    /// </summary>
    /// <param name="plugin">
    /// The plugin instance to add to the discovery process.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder WithPlugin(IServiceDiscoveryPlugin plugin)
    {
        _configuration.WithPlugin(plugin);
        return this;
    }

    /// <summary>
    /// Adds a service discovery plugin using a type that will be instantiated via dependency injection.
    /// This allows plugins to have their own dependencies injected during construction.
    /// </summary>
    /// <typeparam name="TPlugin">
    /// The type of plugin to add.
    /// </typeparam>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder WithPlugin<TPlugin>()
        where TPlugin : class, IServiceDiscoveryPlugin
    {
        _configuration.WithPlugin<TPlugin>();
        return this;
    }

    /// <summary>
    /// Enables or disables performance optimizations such as caching and parallel processing.
    /// Performance optimizations can significantly improve discovery speed but may use more memory.
    /// </summary>
    /// <param name="enabled">
    /// True to enable performance optimizations, false to disable them.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder WithPerformanceOptimizations(bool enabled = true)
    {
        _configuration.WithPerformanceOptimizations(enabled);
        return this;
    }

    /// <summary>
    /// Enables or disables detailed logging of the discovery process.
    /// Logging provides visibility into what services are being discovered and registered.
    /// </summary>
    /// <param name="enabled">
    /// True to enable detailed logging, false to disable it.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder WithLogging(bool enabled = true)
    {
        _configuration.WithLogging(enabled);
        return this;
    }

    /// <summary>
    /// Configures the discovery process for test environments.
    /// This method applies test-specific settings such as excluding services marked with
    /// IgnoreInTests and optimizing for test execution speed over caching.
    /// </summary>
    /// <param name="isTestEnvironment">
    /// True if running in a test environment, false otherwise.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder ForTestEnvironment(bool isTestEnvironment = true)
    {
        _configuration.ForTestEnvironment(isTestEnvironment);
        return this;
    }

    /// <summary>
    /// Provides an IConfiguration instance for conditional service registration.
    /// This configuration will be used to evaluate ConditionalServiceAttribute requirements
    /// and other configuration-based conditions.
    /// </summary>
    /// <param name="configuration">
    /// The IConfiguration instance containing application settings.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder WithConfiguration(IConfiguration configuration)
    {
        _configuration.WithConfiguration(configuration);
        return this;
    }

    /// <summary>
    /// Adds a custom configuration validation rule that will be checked before discovery begins.
    /// Validation rules help ensure that the configuration is complete and consistent
    /// before attempting service discovery.
    /// </summary>
    /// <param name="validationRule">
    /// A function that receives the configuration context and returns a validation result.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    public FluentAutoServiceConfigurationBuilder WithValidation(Func<ConfigurationContext, ConfigurationValidationResult> validationRule)
    {
        _configuration.WithValidation(validationRule);
        return this;
    }

    /// <summary>
    /// Applies the configured settings to perform service discovery and registration.
    /// 
    /// This method represents the culmination of the fluent configuration process. It takes all
    /// the configuration choices that have been built up through the method chain and executes
    /// the actual service discovery and registration process. Think of this method as the "execute"
    /// button that transforms the configuration blueprint into actual service registrations.
    /// 
    /// The application process involves several important steps. First, the fluent configuration
    /// is validated to ensure it contains all necessary information and doesn't have any conflicts.
    /// Then, the configuration is converted into the standard AutoServiceOptions format that the
    /// core discovery system understands. Finally, the discovery process is executed using the
    /// appropriate discovery method based on the configuration choices.
    /// 
    /// This method handles the integration between the fluent configuration system and the existing
    /// service discovery infrastructure, ensuring that all the advanced configuration options are
    /// properly translated and applied during the discovery process.
    /// </summary>
    /// <returns>
    /// The original service collection with all discovered services registered. This allows for
    /// continued method chaining if additional service registrations are needed.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration validation fails or when the discovery process encounters
    /// critical errors that prevent successful service registration.
    /// </exception>
    public IServiceCollection Apply()
    {
        // Build the options from the fluent configuration
        var options = _configuration.BuildOptions();
        
        // Get the assemblies to scan
        var assemblies = _configuration.Assemblies.ToArray();
        
        // If no assemblies were specified, default to the calling assembly
        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetCallingAssembly()];
        }

        // Apply the configuration using the appropriate discovery method
        if (options.EnablePerformanceOptimizations)
        {
            return _services.AddAutoServicesWithPerformanceOptimizations(
                opts => CopyOptionsToTarget(options, opts), 
                assemblies);
        }
        else
        {
            return _services.AddAutoServices(
                opts => CopyOptionsToTarget(options, opts), 
                assemblies);
        }
    }

    /// <summary>
    /// Copies configuration values from the fluent options to the target options object.
    /// This method handles the translation between the fluent configuration format and
    /// the standard AutoServiceOptions format used by the core discovery system.
    /// </summary>
    /// <param name="source">The options built from the fluent configuration.</param>
    /// <param name="target">The target options object to populate.</param>
    private static void CopyOptionsToTarget(AutoServiceOptions source, AutoServiceOptions target)
    {
        target.Profile = source.Profile;
        target.IsTestEnvironment = source.IsTestEnvironment;
        target.EnableLogging = source.EnableLogging;
        target.Configuration = source.Configuration;
        target.EnablePerformanceOptimizations = source.EnablePerformanceOptimizations;
        target.EnableParallelProcessing = source.EnableParallelProcessing;
        target.EnablePerformanceMetrics = source.EnablePerformanceMetrics;
        target.CustomCache = source.CustomCache;
        target.MaxDegreeOfParallelism = source.MaxDegreeOfParallelism;
        target.EnablePlugins = source.EnablePlugins;
    }
}