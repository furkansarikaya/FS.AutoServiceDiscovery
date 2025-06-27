namespace FS.AutoServiceDiscovery.Extensions.Architecture.Conventions;

/// <summary>
/// Defines a contract for resolving service types using multiple naming conventions in a coordinated manner.
/// 
/// This interface addresses a fundamental challenge in extensible systems: when you have multiple
/// strategies that could potentially solve the same problem, how do you coordinate them effectively?
/// 
/// Think of this interface as a "coordinator" or "orchestrator" that manages a team of specialists
/// (naming conventions). Just like a project manager who knows which team member to assign to
/// which task based on their expertise, this resolver knows which naming convention is most
/// likely to succeed for a given type.
/// 
/// The resolver pattern is particularly valuable here because:
/// 1. It provides a single, simple interface for consumers who don't want to manage multiple conventions
/// 2. It implements the ordering and fallback logic so individual conventions can focus on their specialty
/// 3. It can implement performance optimizations like caching successful matches
/// 4. It provides a place to implement cross-convention validation and conflict resolution
/// </summary>
public interface INamingConventionResolver
{
    /// <summary>
    /// Attempts to resolve the most appropriate service interface type for the given implementation type
    /// by trying all registered naming conventions in priority order.
    /// 
    /// This method implements the "chain of responsibility" pattern, where each naming convention
    /// gets a chance to resolve the service type. The first convention that successfully returns
    /// a match wins, and no further conventions are consulted.
    /// 
    /// The resolution process works as follows:
    /// 1. Filter available interfaces to exclude system types
    /// 2. Sort conventions by priority (lower numbers = higher priority)
    /// 3. For each convention, check if it can apply to this type
    /// 4. If applicable, attempt resolution
    /// 5. Return the first successful match
    /// 6. If no convention succeeds, return null
    /// 
    /// This approach balances performance with flexibility - more specific conventions can be
    /// given higher priority to ensure they're tried first, while still allowing fallback to
    /// more general conventions.
    /// </summary>
    /// <param name="implementationType">
    /// The concrete implementation class that needs to be matched with a service interface.
    /// This type has already been validated to be a concrete, non-abstract class.
    /// </param>
    /// <param name="availableInterfaces">
    /// The collection of interfaces actually implemented by the type, pre-filtered to exclude
    /// system interfaces. This represents the "search space" for resolution.
    /// </param>
    /// <returns>
    /// The most appropriate service interface type according to the highest-priority naming
    /// convention that could resolve this type, or null if no convention could determine
    /// an appropriate match.
    /// </returns>
    Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces);
    
    /// <summary>
    /// Gets statistics about the performance and effectiveness of the naming convention resolution process.
    /// 
    /// These statistics help developers understand:
    /// - Which naming conventions are most frequently successful
    /// - Whether the convention priority ordering is optimal
    /// - Performance characteristics of the resolution process
    /// - Potential areas for optimization or additional conventions
    /// 
    /// This information is particularly valuable in large applications where understanding
    /// the service resolution patterns can help optimize the system's configuration.
    /// </summary>
    /// <returns>
    /// Statistical information about naming convention usage and performance.
    /// </returns>
    NamingConventionStatistics GetStatistics();
    
    /// <summary>
    /// Gets the ordered collection of naming conventions currently registered with this resolver.
    /// 
    /// This method provides visibility into the resolver's configuration, which is useful for:
    /// - Debugging resolution issues
    /// - Understanding why certain types resolve the way they do
    /// - Validating that custom conventions are properly registered
    /// - Performance analysis and optimization
    /// </summary>
    /// <returns>
    /// The naming conventions in the order they will be evaluated (highest priority first).
    /// </returns>
    IEnumerable<INamingConvention> GetRegisteredConventions();
}