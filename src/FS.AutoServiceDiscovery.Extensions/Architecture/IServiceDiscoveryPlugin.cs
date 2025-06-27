using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Architecture;

/// <summary>
/// Defines a contract for service discovery plugins that can extend the core discovery functionality.
/// This interface enables third-party libraries and custom extensions to participate in the service
/// discovery process without modifying the core library code.
/// 
/// Think of this interface as a standardized "extension point" - like USB ports on a computer.
/// Just as USB ports allow different devices to connect and extend computer functionality,
/// this interface allows different discovery strategies to plug into our system.
/// 
/// The plugin architecture solves several important problems:
/// 1. Extensibility without modification - new discovery rules can be added without changing core code
/// 2. Third-party integration - libraries like MediatR, FluentValidation can provide their own discovery logic
/// 3. Custom business rules - organizations can implement company-specific discovery patterns
/// 4. Testing and experimentation - new discovery strategies can be developed and tested independently
/// </summary>
public interface IServiceDiscoveryPlugin
{
    /// <summary>
    /// Gets the unique name identifier for this plugin.
    /// This name is used for logging, debugging, and configuration purposes.
    /// 
    /// Choose names that are descriptive and unlikely to conflict with other plugins.
    /// Good examples: "MediatR.Handlers", "FluentValidation.Validators", "Company.CustomRules"
    /// 
    /// The name serves several purposes:
    /// - Debugging: When issues occur, logs can show which plugin caused problems
    /// - Configuration: Users can enable/disable specific plugins by name
    /// - Ordering: Plugins can be processed in specific orders based on their names
    /// </summary>
    /// <value>A unique, descriptive name for this plugin</value>
    string Name { get; }
    
    /// <summary>
    /// Gets the execution priority for this plugin when multiple plugins are present.
    /// Plugins with lower priority values are executed first, allowing for ordered processing.
    /// 
    /// This ordering system is crucial when plugins have dependencies on each other or when
    /// certain discovery rules should take precedence over others. For example:
    /// - Core service discovery might run at priority 0
    /// - Framework-specific discoveries (like MediatR) might run at priority 10
    /// - Custom business rules might run at priority 20
    /// 
    /// This approach prevents conflicts and ensures predictable behavior when multiple
    /// plugins are modifying the same services or competing for registration rights.
    /// </summary>
    /// <value>
    /// The execution priority where lower values indicate higher priority.
    /// Default priority should typically be 100 to allow core functionality to run first.
    /// </value>
    int Priority { get; }
    
    /// <summary>
    /// Determines whether this plugin can process the specified assembly.
    /// This method acts as a "filter" to avoid unnecessary processing overhead.
    /// 
    /// Think of this method as a bouncer at a club - it quickly determines whether
    /// someone (an assembly) meets the criteria for entry (processing by this plugin).
    /// 
    /// Fast filtering is crucial for performance because:
    /// 1. Assembly inspection can be expensive
    /// 2. Plugins shouldn't waste time on irrelevant assemblies
    /// 3. Early filtering reduces the overall discovery time
    /// 
    /// Common filtering strategies include:
    /// - Assembly name patterns (e.g., assemblies starting with "MyCompany.")
    /// - Presence of specific references (e.g., assemblies that reference MediatR)
    /// - Assembly attributes or metadata
    /// - File location or deployment characteristics
    /// </summary>
    /// <param name="assembly">The assembly to evaluate for processing eligibility</param>
    /// <returns>
    /// True if this plugin should process the assembly, false to skip it entirely.
    /// Returning false helps optimize performance by avoiding unnecessary discovery operations.
    /// </returns>
    bool CanProcessAssembly(Assembly assembly);
    
    /// <summary>
    /// Discovers and returns service registration information for the specified assembly.
    /// This is the core method where the plugin implements its custom discovery logic.
    /// 
    /// This method is where the real magic happens - it's like a specialized detective
    /// that knows how to find specific types of evidence (services) that the standard
    /// discovery process might miss or handle differently.
    /// 
    /// The method should be designed with these principles in mind:
    /// 
    /// Performance Considerations:
    /// - Use efficient filtering to avoid examining irrelevant types
    /// - Cache reflection results when possible
    /// - Consider parallel processing for large assemblies
    /// 
    /// Reliability Considerations:
    /// - Handle ReflectionTypeLoadException gracefully
    /// - Never throw exceptions that would break the entire discovery process
    /// - Return empty results rather than failing catastrophically
    /// 
    /// Integration Considerations:
    /// - Respect existing ServiceRegistrationAttribute if present
    /// - Provide clear logging about what was discovered and why
    /// - Consider how your discoveries interact with other plugins
    /// </summary>
    /// <param name="assembly">
    /// The assembly to scan for services. This assembly has already passed the
    /// CanProcessAssembly filter, so you can assume it's relevant to your plugin.
    /// </param>
    /// <param name="options">
    /// The current discovery options including profile, configuration, and feature flags.
    /// Use these to make your discovery context-aware and consistent with global settings.
    /// </param>
    /// <returns>
    /// A collection of ServiceRegistrationInfo objects representing the services discovered
    /// by this plugin. Return an empty collection (not null) if no services are found.
    /// 
    /// Each ServiceRegistrationInfo should be fully populated with:
    /// - ServiceType: The interface or type to register
    /// - ImplementationType: The concrete class providing the implementation
    /// - Lifetime: Appropriate service lifetime for the discovered service
    /// - Order: Registration order if it matters for your plugin's services
    /// - Additional metadata: Profile, conditional attributes, etc.
    /// </returns>
    IEnumerable<ServiceRegistrationInfo> DiscoverServices(Assembly assembly, AutoServiceOptions options);
    
    /// <summary>
    /// Validates that the discovered services are consistent and won't cause conflicts.
    /// This method provides an opportunity to perform cross-service validation and conflict resolution.
    /// 
    /// This validation phase is crucial for maintaining system integrity because:
    /// 
    /// Conflict Detection:
    /// - Multiple plugins might discover the same service type
    /// - Services might have incompatible lifetimes
    /// - Circular dependencies might be created
    /// 
    /// Business Rule Validation:
    /// - Ensure required services are present
    /// - Validate that service combinations make sense
    /// - Check that conditional requirements are met
    /// 
    /// Performance Optimization:
    /// - Remove duplicate registrations
    /// - Optimize service ordering
    /// - Flag potential performance issues
    /// 
    /// The validation results help users understand what happened during discovery
    /// and provide actionable information for resolving any issues found.
    /// </summary>
    /// <param name="discoveredServices">
    /// All services discovered by this plugin across all processed assemblies.
    /// This gives you a complete view of what your plugin contributed to the system.
    /// </param>
    /// <param name="allServices">
    /// All services discovered by all plugins, including the core discovery process.
    /// Use this for cross-plugin validation and conflict detection.
    /// </param>
    /// <param name="options">
    /// The discovery options for context-aware validation.
    /// </param>
    /// <returns>
    /// A validation result indicating whether the discovered services are valid and providing
    /// details about any issues found. The system will use these results to decide whether
    /// to proceed with registration or report errors to the user.
    /// </returns>
    PluginValidationResult ValidateDiscoveredServices(
        IEnumerable<ServiceRegistrationInfo> discoveredServices,
        IEnumerable<ServiceRegistrationInfo> allServices,
        AutoServiceOptions options);
}