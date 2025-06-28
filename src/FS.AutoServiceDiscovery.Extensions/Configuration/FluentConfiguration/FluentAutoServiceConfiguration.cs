using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Architecture;
using FS.AutoServiceDiscovery.Extensions.Architecture.Conventions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FS.AutoServiceDiscovery.Extensions.Configuration.FluentConfiguration;

/// <summary>
/// Provides a fluent interface implementation for configuring auto service discovery with method chaining support.
/// 
/// This class represents the concrete implementation of the fluent configuration API that enables developers
/// to build complex service discovery configurations using a natural, readable syntax. Think of this class
/// as a sophisticated configuration builder that remembers each setting you apply and combines them all
/// into a coherent configuration when you're ready to execute the discovery process.
/// 
/// The fluent pattern implemented here allows for intuitive configuration that reads almost like natural
/// language. For example: "From these assemblies, with this profile, when this condition is met, exclude
/// these types" becomes a chain of method calls that directly express the developer's intent.
/// 
/// This implementation maintains all configuration state internally and validates the consistency of
/// settings as they are applied, providing early feedback about potential configuration conflicts.
/// </summary>
public class FluentAutoServiceConfiguration : IFluentAutoServiceConfiguration
{
    private readonly List<Assembly> _assemblies = [];
    private readonly List<Func<Type, bool>> _typeFilters = [];
    private readonly List<Func<Type, bool>> _includeFilters = [];
    private readonly List<string> _excludedNamespaces = [];
    private readonly List<Func<ConfigurationContext, bool>> _conditions = [];
    private readonly List<INamingConvention> _namingConventions = [];
    private readonly List<Type> _namingConventionTypes = [];
    private readonly List<IServiceDiscoveryPlugin> _plugins = [];
    private readonly List<Type> _pluginTypes = [];
    private readonly List<Func<ConfigurationContext, ConfigurationValidationResult>> _validationRules = [];

    private string? _profile;
    private Func<ConfigurationContext, string>? _profileResolver;
    private ServiceLifetime? _defaultLifetime;
    private bool _performanceOptimizations = true;
    private bool _enableLogging = true;
    private bool _isTestEnvironment;
    private IConfiguration? _configuration;

    /// <summary>
    /// Gets the configuration context that provides runtime information for configuration decisions.
    /// This context is continuously updated as the configuration is built and provides the foundation
    /// for conditional logic and validation rules.
    /// </summary>
    public ConfigurationContext Context { get; private set; } = new();

    /// <summary>
    /// Gets the read-only collection of assemblies that have been configured for discovery.
    /// This collection represents the scope of the discovery operation and determines which
    /// assemblies will be scanned for services.
    /// </summary>
    public IReadOnlyCollection<Assembly> Assemblies => _assemblies.AsReadOnly();

    /// <summary>
    /// Specifies the assemblies to scan for service discovery.
    /// This method establishes the fundamental scope of the discovery operation by defining
    /// which assemblies should be examined for services marked with registration attributes.
    /// </summary>
    /// <param name="assemblies">
    /// The assemblies to include in the discovery process. These assemblies will be scanned
    /// for classes marked with ServiceRegistrationAttribute and related attributes.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    public IFluentAutoServiceConfiguration FromAssemblies(params Assembly[] assemblies)
    {
        if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

        foreach (var assembly in assemblies.Where(a => a != null))
        {
            if (!_assemblies.Contains(assembly))
            {
                _assemblies.Add(assembly);
            }
        }

        UpdateContext();
        return this;
    }

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
    public IFluentAutoServiceConfiguration FromAssemblyPaths(params string[] assemblyPaths)
    {
        if (assemblyPaths == null) throw new ArgumentNullException(nameof(assemblyPaths));

        var loadedAssemblies = new List<Assembly>();

        foreach (var path in assemblyPaths.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            try
            {
                var assembly = Assembly.LoadFrom(path);
                loadedAssemblies.Add(assembly);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load assembly from path '{path}': {ex.Message}", ex);
            }
        }

        return FromAssemblies(loadedAssemblies.ToArray());
    }

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
    public IFluentAutoServiceConfiguration FromCurrentDomain(Func<Assembly, bool>? assemblyFilter = null)
    {
        var defaultFilter = assemblyFilter ?? (assembly => 
            !assembly.IsDynamic && 
            !string.IsNullOrEmpty(assembly.Location) &&
            !assembly.FullName?.StartsWith("System.", StringComparison.OrdinalIgnoreCase) == true &&
            !assembly.FullName?.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) == true);

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(defaultFilter)
            .ToArray();

        return FromAssemblies(assemblies);
    }

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
    public IFluentAutoServiceConfiguration WithProfile(string profile)
    {
        _profile = profile;
        _profileResolver = null; // Clear any existing resolver
        UpdateContext();
        return this;
    }

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
    public IFluentAutoServiceConfiguration WithProfile(Func<ConfigurationContext, string> profileResolver)
    {
        _profileResolver = profileResolver ?? throw new ArgumentNullException(nameof(profileResolver));
        _profile = null; // Clear any static profile
        UpdateContext();
        return this;
    }

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
    public IFluentAutoServiceConfiguration When(Func<ConfigurationContext, bool> condition)
    {
        if (condition == null) throw new ArgumentNullException(nameof(condition));
        
        _conditions.Add(condition);
        return this;
    }

    /// <summary>
    /// Adds a conditional constraint based on a simple configuration value check.
    /// This is a convenience method for the common scenario of checking configuration values.
    /// </summary>
    /// <param name="configurationKey">The configuration key to check.</param>
    /// <param name="expectedValue">The expected value for the configuration key.</param>
    /// <returns>The configuration instance for method chaining.</returns>
    public IFluentAutoServiceConfiguration When(string configurationKey, string expectedValue)
    {
        if (string.IsNullOrWhiteSpace(configurationKey)) throw new ArgumentException("Configuration key cannot be null or empty.", nameof(configurationKey));
        if (expectedValue == null) throw new ArgumentNullException(nameof(expectedValue));

        return When(ctx => string.Equals(ctx.Configuration?[configurationKey], expectedValue, StringComparison.OrdinalIgnoreCase));
    }

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
    public IFluentAutoServiceConfiguration ExcludeTypes(Func<Type, bool> typeFilter)
    {
        if (typeFilter == null) throw new ArgumentNullException(nameof(typeFilter));
        
        _typeFilters.Add(typeFilter);
        return this;
    }

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
    public IFluentAutoServiceConfiguration IncludeOnlyTypes(Func<Type, bool> typeFilter)
    {
        if (typeFilter == null) throw new ArgumentNullException(nameof(typeFilter));
        
        _includeFilters.Add(typeFilter);
        return this;
    }

    /// <summary>
    /// Excludes types from specific namespaces from service discovery.
    /// This is a convenience method for namespace-based filtering.
    /// </summary>
    /// <param name="namespaces">
    /// The namespaces to exclude. Types in these namespaces will not be registered.
    /// Supports both exact matches and wildcard patterns (e.g., "MyApp.Internal.*").
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    public IFluentAutoServiceConfiguration ExcludeNamespaces(params string[] namespaces)
    {
        if (namespaces == null) throw new ArgumentNullException(nameof(namespaces));
        
        foreach (var ns in namespaces.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            if (!_excludedNamespaces.Contains(ns))
            {
                _excludedNamespaces.Add(ns);
            }
        }

        // Convert namespace exclusions to type filters
        foreach (var ns in namespaces)
        {
            var namespaceName = ns.EndsWith("*") ? ns[..^1] : ns;
            var isWildcard = ns.EndsWith("*");
            
            ExcludeTypes(type => 
                type.Namespace != null && (
                    (isWildcard && type.Namespace.StartsWith(namespaceName, StringComparison.OrdinalIgnoreCase)) ||
                    (!isWildcard && string.Equals(type.Namespace, namespaceName, StringComparison.OrdinalIgnoreCase))
                ));
        }
        
        return this;
    }

    /// <summary>
    /// Sets a default service lifetime for services that don't specify one explicitly.
    /// This provides a fallback lifetime for services that don't have explicit lifetime
    /// configuration in their registration attributes.
    /// </summary>
    /// <param name="lifetime">
    /// The default service lifetime to use (Singleton, Scoped, or Transient).
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    public IFluentAutoServiceConfiguration WithDefaultLifetime(ServiceLifetime lifetime)
    {
        _defaultLifetime = lifetime;
        return this;
    }

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
    public IFluentAutoServiceConfiguration WithNamingConvention(INamingConvention convention)
    {
        if (convention == null) throw new ArgumentNullException(nameof(convention));
        
        _namingConventions.Add(convention);
        return this;
    }

    /// <summary>
    /// Adds a custom naming convention using a type that will be instantiated via dependency injection.
    /// This allows naming conventions to have their own dependencies injected during construction.
    /// </summary>
    /// <typeparam name="TNamingConvention">
    /// The type of naming convention to add. Must implement INamingConvention and have
    /// a constructor that can be satisfied by the dependency injection container.
    /// </typeparam>
    /// <returns>The configuration instance for method chaining.</returns>
    public IFluentAutoServiceConfiguration WithNamingConvention<TNamingConvention>()
        where TNamingConvention : class, INamingConvention
    {
        _namingConventionTypes.Add(typeof(TNamingConvention));
        return this;
    }

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
    public IFluentAutoServiceConfiguration WithPlugin(IServiceDiscoveryPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        
        _plugins.Add(plugin);
        return this;
    }

    /// <summary>
    /// Adds a service discovery plugin using a type that will be instantiated via dependency injection.
    /// This allows plugins to have their own dependencies injected during construction.
    /// </summary>
    /// <typeparam name="TPlugin">
    /// The type of plugin to add. Must implement IServiceDiscoveryPlugin and have
    /// a constructor that can be satisfied by the dependency injection container.
    /// </typeparam>
    /// <returns>The configuration instance for method chaining.</returns>
    public IFluentAutoServiceConfiguration WithPlugin<TPlugin>()
        where TPlugin : class, IServiceDiscoveryPlugin
    {
        _pluginTypes.Add(typeof(TPlugin));
        return this;
    }

    /// <summary>
    /// Enables or disables performance optimizations such as caching and parallel processing.
    /// Performance optimizations can significantly improve discovery speed but may use more memory.
    /// </summary>
    /// <param name="enabled">
    /// True to enable performance optimizations, false to disable them.
    /// When enabled, the system will use assembly caching and parallel processing where beneficial.
    /// </param>
    /// <returns>The configuration instance for method chaining.</returns>
    public IFluentAutoServiceConfiguration WithPerformanceOptimizations(bool enabled = true)
    {
        _performanceOptimizations = enabled;
        return this;
    }

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
    public IFluentAutoServiceConfiguration WithLogging(bool enabled = true)
    {
        _enableLogging = enabled;
        return this;
    }

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
    public IFluentAutoServiceConfiguration ForTestEnvironment(bool isTestEnvironment = true)
    {
        _isTestEnvironment = isTestEnvironment;
        UpdateContext();
        return this;
    }

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
    public IFluentAutoServiceConfiguration WithConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
        UpdateContext();
        return this;
    }

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
    public IFluentAutoServiceConfiguration WithValidation(Func<ConfigurationContext, ConfigurationValidationResult> validationRule)
    {
        if (validationRule == null) throw new ArgumentNullException(nameof(validationRule));
        
        _validationRules.Add(validationRule);
        return this;
    }

    /// <summary>
    /// Builds the final AutoServiceOptions from the fluent configuration.
    /// This method consolidates all the configuration choices into the standard options
    /// object that can be used by the discovery system.
    /// </summary>
    /// <returns>
    /// An AutoServiceOptions instance configured according to all the fluent configuration
    /// methods that were called on this builder.
    /// </returns>
    public AutoServiceOptions BuildOptions()
    {
        // Run validation before building
        ValidateConfiguration();

        // Update context one final time
        UpdateContext();

        // Determine the active profile
        var activeProfile = _profileResolver?.Invoke(Context) ?? _profile;

        var options = new AutoServiceOptions
        {
            Profile = activeProfile,
            IsTestEnvironment = _isTestEnvironment,
            EnableLogging = _enableLogging,
            Configuration = _configuration,
            EnablePerformanceOptimizations = _performanceOptimizations,
            EnableParallelProcessing = _performanceOptimizations,
            EnablePerformanceMetrics = _performanceOptimizations
        };

        // Store additional configuration data in custom properties for use by the discovery process
        Context.SetProperty("TypeFilters", _typeFilters);
        Context.SetProperty("IncludeFilters", _includeFilters);
        Context.SetProperty("DefaultLifetime", _defaultLifetime ?? ServiceLifetime.Scoped);
        Context.SetProperty("NamingConventions", _namingConventions);
        Context.SetProperty("NamingConventionTypes", _namingConventionTypes);
        Context.SetProperty("Plugins", _plugins);
        Context.SetProperty("PluginTypes", _pluginTypes);
        Context.SetProperty("Conditions", _conditions);

        return options;
    }

    /// <summary>
    /// Updates the configuration context with current settings.
    /// This method ensures that the context always reflects the latest configuration state
    /// for use by conditional expressions and validation rules.
    /// </summary>
    private void UpdateContext()
    {
        Context.Configuration = _configuration;
        Context.IsTestEnvironment = _isTestEnvironment;
        Context.ActiveProfile = _profileResolver?.Invoke(Context) ?? _profile;
        Context.SetProperty("AssemblyCount", _assemblies.Count);
        Context.SetProperty("HasTypeFilters", _typeFilters.Count != 0 || _includeFilters.Count != 0);
        Context.SetProperty("HasConditions", _conditions.Count != 0);
    }

    /// <summary>
    /// Validates the current configuration using all registered validation rules.
    /// This method runs all validation rules and throws an exception if any critical
    /// validation errors are found.
    /// </summary>
    private void ValidateConfiguration()
    {
        var overallResult = new ConfigurationValidationResult();

        // Run built-in validations
        overallResult.Merge(ValidateBasicConfiguration());

        // Run custom validation rules
        foreach (var rule in _validationRules)
        {
            try
            {
                var result = rule(Context);
                overallResult.Merge(result);
            }
            catch (Exception ex)
            {
                overallResult.AddError($"Validation rule failed with exception: {ex.Message}");
            }
        }

        // If there are critical errors, throw an exception
        if (!overallResult.IsValid)
        {
            var errorReport = overallResult.GenerateReport();
            throw new InvalidOperationException($"Configuration validation failed:\n{errorReport}");
        }
    }

    /// <summary>
    /// Performs basic validation of the configuration to catch common issues.
    /// This method checks for fundamental configuration problems that would prevent
    /// the discovery system from working correctly.
    /// </summary>
    /// <returns>A validation result indicating whether the basic configuration is valid.</returns>
    private ConfigurationValidationResult ValidateBasicConfiguration()
    {
        var result = ConfigurationValidationResult.Success("Basic Configuration");

        // Check if at least one assembly is specified
        if (_assemblies.Count == 0)
        {
            result.AddError("No assemblies specified for discovery. Use FromAssemblies(), FromAssemblyPaths(), or FromCurrentDomain().");
        }

        // Check for conflicting include/exclude filters
        if (_includeFilters.Count != 0 && _typeFilters.Count != 0)
        {
            result.AddWarning("Both include and exclude type filters are specified. Include filters take precedence, so exclude filters may have no effect.");
        }

        // Validate that assemblies can be loaded and accessed
        foreach (var assembly in _assemblies)
        {
            try
            {
                _ = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                result.AddWarning($"Assembly '{assembly.GetName().Name}' has type loading issues. Some types may be skipped: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.AddError($"Assembly '{assembly.GetName().Name}' cannot be processed: {ex.Message}");
            }
        }

        // Add informational summary
        result.AddInformation($"Configuration summary: {_assemblies.Count} assemblies, {_typeFilters.Count} exclude filters, {_includeFilters.Count} include filters, {_conditions.Count} conditions");

        return result;
    }
}