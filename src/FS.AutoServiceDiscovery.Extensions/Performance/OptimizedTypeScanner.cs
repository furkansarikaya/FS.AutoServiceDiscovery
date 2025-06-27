using System.Collections.Concurrent;
using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Attributes;
using FS.AutoServiceDiscovery.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Performance;

/// <summary>
/// High-performance type scanner that uses parallel processing and caching to optimize assembly scanning.
/// This scanner implements several optimization strategies:
/// 1. Parallel processing for multiple assemblies
/// 2. Type metadata caching to avoid repeated reflection calls
/// 3. Early filtering to reduce processing overhead
/// 4. Bulk operations to minimize allocations
/// </summary>
public class OptimizedTypeScanner
{
    // Cache for type metadata to avoid repeated reflection calls
    // This is particularly beneficial for types that are scanned multiple times
    private static readonly ConcurrentDictionary<Type, TypeMetadata> TypeMetadataCache = new();
    
    /// <summary>
    /// Cached metadata for a type to avoid repeated reflection operations.
    /// </summary>
    private class TypeMetadata
    {
        public ServiceRegistrationAttribute? RegistrationAttribute { get; set; }
        public ConditionalServiceAttribute[] ConditionalAttributes { get; set; } = [];
        public Type[] Interfaces { get; set; } = [];
        public bool IsServiceCandidate { get; set; }
    }
    
    /// <summary>
    /// Scans multiple assemblies in parallel for service candidates.
    /// This method is optimized for scenarios with multiple large assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan</param>
    /// <returns>Service registration information for all discovered services</returns>
    public IEnumerable<ServiceRegistrationInfo> ScanAssemblies(IEnumerable<Assembly> assemblies)
    {
        var assemblyList = assemblies.ToList();
        
        // For small numbers of assemblies, parallel processing overhead might not be worth it
        if (assemblyList.Count <= 2)
        {
            return assemblyList.SelectMany(ScanAssemblySequential);
        }
        
        // Use parallel processing for better performance with multiple assemblies
        // The AsParallel() call creates a PLINQ query that can utilize multiple CPU cores
        return assemblyList.AsParallel()
            .WithDegreeOfParallelism(Environment.ProcessorCount) // Limit to CPU core count
            .SelectMany(ScanAssemblyOptimized)
            .ToList(); // Materialize to avoid deferred execution issues
    }
    
    /// <summary>
    /// Optimized scanning for a single assembly with early filtering and bulk operations.
    /// </summary>
    private IEnumerable<ServiceRegistrationInfo> ScanAssemblyOptimized(Assembly assembly)
    {
        try
        {
            // Get all types from assembly in one call to minimize reflection overhead
            var allTypes = assembly.GetTypes();
            
            // Early filtering: eliminate obviously non-service types quickly
            // This reduces the number of types we need to examine in detail
            var candidateTypes = allTypes.Where(IsTypeCandidate).ToList();
            
            if (candidateTypes.Count == 0)
                return [];
            
            // Process candidate types and extract service information
            var results = new List<ServiceRegistrationInfo>(candidateTypes.Count);
            
            foreach (var type in candidateTypes)
            {
                var metadata = GetOrCreateTypeMetadata(type);

                if (metadata is not { IsServiceCandidate: true, RegistrationAttribute: not null }) 
                    continue;
                var serviceInfo = CreateServiceRegistrationInfo(type, metadata);
                if (serviceInfo != null)
                {
                    results.Add(serviceInfo);
                }
            }
            
            return results;
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Handle cases where some types in the assembly can't be loaded
            // This is common with assemblies that have missing dependencies
            var loadableTypes = ex.Types.Where(t => t != null).Cast<Type>();
            
            var validTypes = new List<ServiceRegistrationInfo>();
            
            foreach (var type in loadableTypes.Where(IsTypeCandidate))
            {
                var metadata = GetOrCreateTypeMetadata(type);
                if (metadata is not { IsServiceCandidate: true, RegistrationAttribute: not null }) 
                    continue;
                var serviceInfo = CreateServiceRegistrationInfo(type, metadata);
                if (serviceInfo != null)
                {
                    validTypes.Add(serviceInfo);
                }
            }
            
            return validTypes;
        }
        catch
        {
            // For any other reflection errors, return empty results rather than crashing
            return [];
        }
    }
    
    /// <summary>
    /// Sequential scanning for single assemblies or fallback scenarios.
    /// </summary>
    private IEnumerable<ServiceRegistrationInfo> ScanAssemblySequential(Assembly assembly)
    {
        return ScanAssemblyOptimized(assembly); // Reuse the optimized logic
    }
    
    /// <summary>
    /// Fast preliminary check to determine if a type could be a service candidate.
    /// This method is designed to be as fast as possible to filter out obviously unsuitable types.
    /// </summary>
    private static bool IsTypeCandidate(Type type)
    {
        // Quick checks that don't require attribute inspection
        if (!type.IsClass) return false;
        if (type.IsAbstract) return false;
        if (type.IsGenericTypeDefinition) return false;
        return type is not { IsNested: true, IsNestedPublic: false } &&
               // Check for our service registration attribute
               // Bu daha pahalı bir işlem ama doğru filtreleme için gerekli
               type.IsDefined(typeof(ServiceRegistrationAttribute), false);
    }
    
    /// <summary>
    /// Gets cached type metadata or creates and caches new metadata for a type.
    /// This method implements our type-level caching strategy to avoid repeated reflection calls.
    /// </summary>
    private static TypeMetadata GetOrCreateTypeMetadata(Type type)
    {
        return TypeMetadataCache.GetOrAdd(type, CreateTypeMetadata);
    }
    
    /// <summary>
    /// Creates comprehensive metadata for a type using reflection.
    /// This method performs all the expensive reflection operations once and caches the results.
    /// </summary>
    private static TypeMetadata CreateTypeMetadata(Type type)
    {
        var metadata = new TypeMetadata
        {
            // Get service registration attribute
            RegistrationAttribute = type.GetCustomAttribute<ServiceRegistrationAttribute>()
        };

        metadata.IsServiceCandidate = metadata.RegistrationAttribute != null;
        
        if (!metadata.IsServiceCandidate)
            return metadata;
        
        // Get conditional attributes - bunlar feature flag bazlı registration için kullanılır
        metadata.ConditionalAttributes = type.GetCustomAttributes<ConditionalServiceAttribute>().ToArray();
        
        // Get interfaces - service type resolution için kullanılır
        metadata.Interfaces = type.GetInterfaces()
            .Where(i => !i.Name.StartsWith("System.")) // System interface'lerini filtrele
            .ToArray();
        
        return metadata;
    }
    
    /// <summary>
    /// Creates service registration information from type metadata.
    /// This method implements the service type resolution logic efficiently.
    /// </summary>
    private static ServiceRegistrationInfo? CreateServiceRegistrationInfo(Type implementationType, TypeMetadata metadata)
    {
        if (metadata.RegistrationAttribute == null)
            return null;
        
        var serviceType = DetermineServiceType(implementationType, metadata);
        if (serviceType == null)
            return null;
        
        return new ServiceRegistrationInfo
        {
            ServiceType = serviceType,
            ImplementationType = implementationType,
            Lifetime = metadata.RegistrationAttribute.Lifetime,
            Order = metadata.RegistrationAttribute.Order,
            Profile = metadata.RegistrationAttribute.Profile,
            IgnoreInTests = metadata.RegistrationAttribute.IgnoreInTests,
            ConditionalAttributes = metadata.ConditionalAttributes
        };
    }
    
    /// <summary>
    /// Optimized service type determination using cached interface information.
    /// </summary>
    private static Type? DetermineServiceType(Type implementationType, TypeMetadata metadata)
    {
        var attribute = metadata.RegistrationAttribute!;
        
        // Eğer explicit service type belirtilmişse, onu kullan
        if (attribute.ServiceType != null)
            return attribute.ServiceType;
        
        // Convention: I{ClassName} interface'ini ara
        var interfaceName = $"I{implementationType.Name}";
        var conventionInterface = metadata.Interfaces.FirstOrDefault(i => i.Name == interfaceName);
        
        if (conventionInterface != null)
            return conventionInterface;
        
        // Eğer sadece bir interface varsa, onu kullan
        if (metadata.Interfaces.Length == 1)
            return metadata.Interfaces[0];
        
        // Fallback olarak concrete type'ı kullan
        return implementationType;
    }
    
    /// <summary>
    /// Clears the type metadata cache. Useful for testing scenarios or when assemblies are reloaded.
    /// </summary>
    public static void ClearCache()
    {
        TypeMetadataCache.Clear();
    }
    
    /// <summary>
    /// Gets cache statistics for monitoring purposes.
    /// </summary>
    public static int GetCachedTypeCount()
    {
        return TypeMetadataCache.Count;
    }
}