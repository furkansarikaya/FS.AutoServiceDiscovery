using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Architecture;
using FS.AutoServiceDiscovery.Extensions.Architecture.Conventions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FS.AutoServiceDiscovery.Extensions.Configuration.FluentConfiguration;

/// <summary>
/// Defines a fluent interface for configuring auto service discovery with method chaining support.
/// 
/// This interface enables a declarative, readable approach to configuring service discovery behavior.
/// Think of it as building a sentence in natural language - each method adds another clause that
/// describes how the discovery should behave. This approach makes complex configurations both
/// more readable and more discoverable through IntelliSense.
/// 
/// The fluent pattern allows developers to chain configuration methods together, creating
/// expressive configuration code that reads almost like documentation. For example:
/// "From these assemblies, with this profile, when this condition is met, exclude these types"
/// becomes a chain of method calls that directly express the intent.
/// </summary>
public interface IFluentAutoServiceConfiguration
{
    /// <summary>
    /// Specifies the assemblies to scan for service discovery.
    /// This method establishes the scope of the discovery operation by defining which assemblies
    /// should be examined for services marked with registration attributes.
    /// </summary>
    /// <param name="assemblies">
    /// The assemblies to include in the discovery process. These assemblies will be scanned
    /// for classes marked with ServiceRegistrationAttribute and related attributes.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration FromAssemblies(params Assembly[] assemblies);

    /// <summary>
    /// Specifies assemblies to scan by loading them from the given file paths.
    /// This method provides an alternative way to specify assemblies when you have file paths
    /// rather than Assembly objects, which is common in plugin scenarios or dynamic loading.
    /// </summary>
    /// <param name="assemblyPaths">
    /// File paths to assembly files that should be loaded and scanned for services.
    /// The assemblies will be loaded using Assembly.LoadFrom().
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration FromAssemblyPaths(params string[] assemblyPaths);

    /// <summary>
    /// Automatically discovers and includes assemblies from the current application domain.
    /// This method scans the current AppDomain for loaded assemblies and includes those
    /// that match the specified filter criteria.
    /// </summary>
    /// <param name="assemblyFilter">
    /// Optional filter to determine which assemblies from the AppDomain should be included.
    /// If not provided, all non-system assemblies will be included.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration FromCurrentDomain(Func<Assembly, bool>? assemblyFilter = null);

    /// <summary>
    /// Sets the active profile for profile-based service registration.
    /// Services marked with specific profiles will only be registered when the active profile
    /// matches their profile configuration.
    /// </summary>
    /// <param name="profile">
    /// The profile name to activate (e.g., "Development", "Production", "Testing").
    /// This should match the Profile property values used in ServiceRegistrationAttribute.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration WithProfile(string profile);

    /// <summary>
    /// Sets the active profile using a dynamic resolver function.
    /// This allows the profile to be determined at runtime based on configuration,
    /// environment variables, or other dynamic factors.
    /// </summary>
    /// <param name="profileResolver">
    /// A function that takes the current configuration context and returns the appropriate
    /// profile name. This enables context-sensitive profile determination.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration WithProfile(Func<ConfigurationContext, string> profileResolver);

    /// <summary>
    /// Adds a conditional constraint that must be satisfied for discovery to proceed.
    /// This method enables complex conditional logic for service registration based on
    /// runtime conditions, configuration values, or other dynamic factors.
    /// </summary>
    /// <param name="condition">
    /// A predicate function that receives the configuration context and returns true
    /// if the condition is satisfied. If false, affected services will be excluded.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration When(Func<ConfigurationContext, bool> condition);

    /// <summary>
    /// Adds a conditional constraint based on a simple configuration value check.
    /// This is a convenience method for the common scenario of checking configuration values.
    /// </summary>
    /// <param name="configurationKey">The configuration key to check.</param>
    /// <param name="expectedValue">The expected value for the configuration key.</param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration When(string configurationKey, string expectedValue);

    /// <summary>
    /// Excludes types that match the specified filter from service discovery.
    /// This method provides fine-grained control over which types should be ignored
    /// during the discovery process, even if they have registration attributes.
    /// </summary>
    /// <param name="typeFilter">
    /// A predicate function that receives a type and returns true if the type should
    /// be excluded from discovery. Excluded types will not be registered as services.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration ExcludeTypes(Func<Type, bool> typeFilter);

    /// <summary>
    /// Includes only types that match the specified filter in service discovery.
    /// This method provides a whitelist approach to type filtering, where only
    /// types that pass the filter will be considered for registration.
    /// </summary>
    /// <param name="typeFilter">
    /// A predicate function that receives a type and returns true if the type should
    /// be included in discovery. Only included types will be registered as services.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration IncludeOnlyTypes(Func<Type, bool> typeFilter);

    /// <summary>
    /// Excludes types from specific namespaces from service discovery.
    /// This is a convenience method for namespace-based filtering.
    /// </summary>
    /// <param name="namespaces">
    /// The namespaces to exclude. Types in these namespaces will not be registered.
    /// Supports both exact matches and wildcard patterns (e.g., "MyApp.Internal.*").
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration ExcludeNamespaces(params string[] namespaces);

    /// <summary>
    /// Sets a default service lifetime for services that don't specify one explicitly.
    /// This provides a fallback lifetime for services that don't have explicit lifetime
    /// configuration in their registration attributes.
    /// </summary>
    /// <param name="lifetime">
    /// The default service lifetime to use (Singleton, Scoped, or Transient).
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration WithDefaultLifetime(ServiceLifetime lifetime);

    /// <summary>
    /// Adds a custom naming convention for service type resolution.
    /// Naming conventions determine how implementation types are matched with service interfaces
    /// when the ServiceType is not explicitly specified in the registration attribute.
    /// </summary>
    /// <param name="convention">
    /// The naming convention instance to add to the resolution process.
    /// Multiple conventions can be added and will be evaluated in priority order.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration WithNamingConvention(INamingConvention convention);

    /// <summary>
    /// Adds a custom naming convention using a type that will be instantiated via dependency injection.
    /// This allows naming conventions to have their own dependencies injected during construction.
    /// </summary>
    /// <typeparam name="TNamingConvention">
    /// The type of naming convention to add. Must implement INamingConvention and have
    /// a constructor that can be satisfied by the dependency injection container.
    /// </typeparam>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration WithNamingConvention<TNamingConvention>()
        where TNamingConvention : class, INamingConvention;

    /// <summary>
    /// Adds a service discovery plugin to extend the discovery process with custom logic.
    /// Plugins allow for specialized discovery strategies that go beyond the standard
    /// attribute-based approach.
    /// </summary>
    /// <param name="plugin">
    /// The plugin instance to add to the discovery process.
    /// Plugins are executed in priority order during discovery.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration WithPlugin(IServiceDiscoveryPlugin plugin);

    /// <summary>
    /// Adds a service discovery plugin using a type that will be instantiated via dependency injection.
    /// This allows plugins to have their own dependencies injected during construction.
    /// </summary>
    /// <typeparam name="TPlugin">
    /// The type of plugin to add. Must implement IServiceDiscoveryPlugin and have
    /// a constructor that can be satisfied by the dependency injection container.
    /// </typeparam>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration WithPlugin<TPlugin>()
        where TPlugin : class, IServiceDiscoveryPlugin;

    /// <summary>
    /// Enables or disables performance optimizations such as caching and parallel processing.
    /// Performance optimizations can significantly improve discovery speed but may use more memory.
    /// </summary>
    /// <param name="enabled">
    /// True to enable performance optimizations, false to disable them.
    /// When enabled, the system will use assembly caching and parallel processing where beneficial.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration WithPerformanceOptimizations(bool enabled = true);

    /// <summary>
    /// Enables or disables detailed logging of the discovery process.
    /// Logging provides visibility into what services are being discovered and registered,
    /// which is valuable for debugging and monitoring.
    /// </summary>
    /// <param name="enabled">
    /// True to enable detailed logging, false to disable it.
    /// When enabled, each discovered and registered service will be logged.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration WithLogging(bool enabled = true);

    /// <summary>
    /// Configures the discovery process for test environments.
    /// This method applies test-specific settings such as excluding services marked with
    /// IgnoreInTests and optimizing for test execution speed over caching.
    /// </summary>
    /// <param name="isTestEnvironment">
    /// True if running in a test environment, false otherwise.
    /// When true, services marked with IgnoreInTests will be excluded.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration ForTestEnvironment(bool isTestEnvironment = true);

    /// <summary>
    /// Provides an IConfiguration instance for conditional service registration.
    /// This configuration will be used to evaluate ConditionalServiceAttribute requirements
    /// and other configuration-based conditions.
    /// </summary>
    /// <param name="configuration">
    /// The IConfiguration instance containing application settings that may be used
    /// for conditional service registration and other configuration-based decisions.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration WithConfiguration(IConfiguration configuration);

    /// <summary>
    /// Adds a custom configuration validation rule that will be checked before discovery begins.
    /// Validation rules help ensure that the configuration is complete and consistent
    /// before attempting service discovery.
    /// </summary>
    /// <param name="validationRule">
    /// A function that receives the configuration context and returns a validation result.
    /// If validation fails, an exception will be thrown with details about the validation failure.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    IFluentAutoServiceConfiguration WithValidation(Func<ConfigurationContext, ConfigurationValidationResult> validationRule);

    /// <summary>
    /// Builds the final AutoServiceOptions from the fluent configuration.
    /// This method consolidates all the configuration choices into the standard options
    /// object that can be used by the discovery system.
    /// </summary>
    /// <returns>
    /// An AutoServiceOptions instance configured according to all the fluent configuration
    /// methods that were called on this builder.
    /// </returns>
    AutoServiceOptions BuildOptions();

    /// <summary>
    /// Gets the assemblies that have been configured for discovery.
    /// This property provides access to the assembly collection for validation or inspection.
    /// </summary>
    IReadOnlyCollection<Assembly> Assemblies { get; }

    /// <summary>
    /// Gets the current configuration context that contains runtime information.
    /// This context is used by conditional expressions and validation rules.
    /// </summary>
    ConfigurationContext Context { get; }
}