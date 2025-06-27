namespace FS.AutoServiceDiscovery.Extensions.Architecture.Conventions;

/// <summary>
/// Implements the standard .NET naming convention where interfaces are prefixed with "I" followed
/// by the implementation class name.
/// 
/// This convention handles the most common naming pattern in .NET applications:
/// - UserService implements IUserService
/// - ProductRepository implements IProductRepository
/// - EmailSender implements IEmailSender
/// 
/// This convention is considered the "gold standard" in .NET development because it follows
/// Microsoft's own naming guidelines and is widely adopted across the ecosystem. It provides
/// clear, predictable mapping between implementations and their contracts.
/// 
/// The convention works by taking the implementation class name and looking for an interface
/// with the same name but prefixed with "I". This approach is both intuitive for developers
/// and reliable for automated discovery systems.
/// </summary>
public class StandardInterfacePrefixConvention : INamingConvention
{
    /// <summary>
    /// Gets the descriptive name of this naming convention.
    /// This name appears in logs and debugging output to help identify which convention
    /// was used for service resolution.
    /// </summary>
    public string Name => "Standard Interface Prefix (I{ClassName})";
    
    /// <summary>
    /// Gets the priority for this convention. Since this is the standard .NET convention,
    /// it gets a relatively high priority (low number) to ensure it's checked early
    /// in the convention evaluation process.
    /// </summary>
    public int Priority => 10;
    
    /// <summary>
    /// Determines whether this convention can potentially apply to the given implementation type.
    /// 
    /// This method performs a quick check to see if there's any possibility that the standard
    /// "I{ClassName}" convention could work for this type. Since this is such a fundamental
    /// convention, it can apply to almost any class, so we primarily check for basic requirements.
    /// 
    /// The main things we verify:
    /// 1. The type has a meaningful name (not null or empty)
    /// 2. The type implements at least one interface
    /// 3. The type is not itself an interface
    /// 
    /// This early filtering helps performance by avoiding unnecessary processing for types
    /// that obviously can't work with this convention.
    /// </summary>
    /// <param name="implementationType">The implementation type to evaluate.</param>
    /// <returns>
    /// True if this convention might work for the type, false if it definitely cannot work.
    /// </returns>
    public bool CanApplyTo(Type implementationType)
    {
        // Basic sanity checks - ensure we have a named class that implements interfaces
        return !string.IsNullOrEmpty(implementationType.Name) 
               && implementationType is { IsClass: true, IsAbstract: false }
               && implementationType.GetInterfaces().Length != 0;
    }
    
    /// <summary>
    /// Attempts to resolve the service interface for an implementation type using the standard
    /// "I{ClassName}" naming convention.
    /// 
    /// This method implements the core logic of the standard naming convention:
    /// 
    /// 1. Take the implementation class name (e.g., "UserService")
    /// 2. Prepend "I" to create the expected interface name (e.g., "IUserService")
    /// 3. Search through the available interfaces for one with this exact name
    /// 4. Return the matching interface, or null if no match is found
    /// 
    /// The method handles several edge cases:
    /// - Class names that already start with "I" (though this is unusual for implementations)
    /// - Generic type names (strips generic type parameters for name matching)
    /// - Multiple interfaces with similar names (returns exact matches only)
    /// 
    /// Performance considerations:
    /// - Uses LINQ FirstOrDefault for efficient searching
    /// - Only examines interface names, not their full metadata
    /// - Short-circuits on the first exact match found
    /// </summary>
    /// <param name="implementationType">
    /// The concrete implementation class for which we need to find the corresponding interface.
    /// </param>
    /// <param name="availableInterfaces">
    /// The interfaces actually implemented by the class. This method will only return
    /// interfaces from this collection.
    /// </param>
    /// <returns>
    /// The interface that matches the "I{ClassName}" pattern, or null if no such interface
    /// is found in the available interfaces collection.
    /// </returns>
    public Type? ResolveServiceType(Type implementationType, IEnumerable<Type> availableInterfaces)
    {
        // Get the implementation class name, handling generic types appropriately
        var className = GetNonGenericTypeName(implementationType);
        
        // Generate the expected interface name using the standard convention
        var expectedInterfaceName = $"I{className}";
        
        // Search for an interface with the expected name
        // We use the simple name comparison because we're working within the same assembly context
        var matchingInterface = availableInterfaces.FirstOrDefault(i => 
            GetNonGenericTypeName(i) == expectedInterfaceName);
        
        return matchingInterface;
    }
    
    /// <summary>
    /// Extracts the non-generic type name from a type, handling both generic and non-generic types appropriately.
    /// 
    /// This helper method is necessary because generic types have names like "Repository`1" instead of just "Repository".
    /// For naming convention purposes, we want to work with the base name without the generic parameter indicators.
    /// 
    /// Examples:
    /// - "UserService" -> "UserService" (no change for non-generic types)
    /// - "Repository`1" -> "Repository" (strips generic parameter indicator)
    /// - "GenericService`2" -> "GenericService" (handles multiple generic parameters)
    /// </summary>
    /// <param name="type">The type to extract the name from.</param>
    /// <returns>The type name without generic parameter indicators.</returns>
    private static string GetNonGenericTypeName(Type type)
    {
        var name = type.Name;
        
        // Remove generic type parameter indicators (e.g., `1, `2, etc.)
        var backtickIndex = name.IndexOf('`');
        if (backtickIndex > 0)
        {
            name = name[..backtickIndex];
        }
        
        return name;
    }
}