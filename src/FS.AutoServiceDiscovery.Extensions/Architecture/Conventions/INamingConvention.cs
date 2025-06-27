namespace FS.AutoServiceDiscovery.Extensions.Architecture.Conventions;

/// <summary>
/// Defines a contract for implementing custom naming conventions that determine how implementation
/// types are matched with their corresponding service interfaces during auto-discovery.
/// 
/// Naming conventions solve a common problem in dependency injection: given a concrete implementation
/// class, how do we determine which interface it should be registered under? Different organizations
/// and projects often have different naming standards, and this interface allows the discovery system
/// to be flexible and adapt to various conventions.
/// 
/// Think of naming conventions as translation rules - they take the "name" of something in one
/// language (implementation class names) and translate it to another language (service interface names).
/// Just as human languages have different grammar rules, software projects have different naming rules.
/// 
/// Examples of different naming conventions:
/// - UserService -> IUserService (standard "I" prefix)
/// - UserServiceImpl -> UserService (implementation suffix convention)
/// - ConcreteUserManager -> IUserManager (concrete prefix convention)
/// - UserOperations -> IUserOperations (exact match convention)
/// </summary>
public interface INamingConvention
{
    /// <summary>
    /// Gets a descriptive name for this naming convention that can be used in logging and debugging.
    /// 
    /// The name should clearly indicate what pattern this convention implements and should be
    /// unique enough to distinguish it from other conventions in the same application.
    /// 
    /// Good examples:
    /// - "Standard Interface Prefix (I{Name})"
    /// - "Implementation Suffix Removal ({Name}Impl -> I{Name})"
    /// - "Concrete Prefix Replacement (Concrete{Name} -> I{Name})"
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the priority order for this convention when multiple conventions are registered.
    /// Conventions with lower priority values are evaluated first, allowing for ordered fallback behavior.
    /// 
    /// Priority ordering is important because:
    /// 1. Some conventions are more specific than others
    /// 2. You might want organization-specific conventions to take precedence over generic ones
    /// 3. Performance optimization - put fast, commonly-matching conventions first
    /// 
    /// Recommended priority ranges:
    /// - 1-10: Highly specific, organization-specific conventions
    /// - 10-50: Framework or library-specific conventions
    /// - 50-100: General-purpose conventions
    /// - 100+: Fallback conventions
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Attempts to determine the appropriate service interface type for the given implementation type
    /// based on this naming convention's rules.
    /// 
    /// This method implements the core logic of the naming convention. It should:
    /// 
    /// 1. Analyze the implementation type's name and characteristics
    /// 2. Generate potential interface names based on the convention's rules
    /// 3. Search for matching interfaces in the implementation type's interface list
    /// 4. Return the best match, or null if no match is found according to this convention
    /// 
    /// Performance considerations:
    /// - This method may be called frequently, so it should be efficient
    /// - Avoid expensive operations like reflection unless necessary
    /// - Consider caching results for repeated lookups of the same types
    /// 
    /// The method should be deterministic - calling it multiple times with the same input
    /// should always produce the same result.
    /// </summary>
    /// <param name="implementationType">
    /// The concrete implementation type that needs to be matched with a service interface.
    /// This type is guaranteed to be a class (not an interface or abstract class) and to
    /// implement at least one interface beyond basic system interfaces.
    /// </param>
    /// <param name="availableInterfaces">
    /// The collection of interfaces that the implementation type actually implements.
    /// This list has already been filtered to exclude system interfaces (those starting
    /// with "System.") to focus on application-specific interfaces.
    /// 
    /// Use this list as your search space - you can only return interfaces that are
    /// actually implemented by the type.
    /// </param>
    /// <returns>
    /// The interface type that this implementation should be registered under according
    /// to this naming convention, or null if this convention doesn't apply to this type.
    /// 
    /// Returning null doesn't indicate an error - it simply means this particular
    /// convention doesn't know how to handle this type, and other conventions should
    /// be tried.
    /// </returns>
    Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces);
    
    /// <summary>
    /// Determines whether this naming convention can potentially apply to the given implementation type.
    /// This method provides an optimization opportunity by allowing conventions to quickly exclude
    /// types they know they cannot handle, avoiding expensive processing.
    /// 
    /// This method acts as a "pre-filter" to improve performance:
    /// - If a convention only works with types ending in "Service", it can quickly return false
    ///   for types that don't match this pattern
    /// - If a convention requires specific attributes or interfaces, it can check for those quickly
    /// - Generic early-exit logic can save significant processing time in large applications
    /// 
    /// The method should err on the side of returning true when uncertain - it's better to
    /// occasionally call ResolveServiceType unnecessarily than to miss a valid match.
    /// </summary>
    /// <param name="implementationType">The implementation type to evaluate.</param>
    /// <returns>
    /// True if this convention might be able to resolve a service type for this implementation,
    /// false if this convention definitely cannot handle this type.
    /// </returns>
    bool CanApplyTo(Type implementationType);
}