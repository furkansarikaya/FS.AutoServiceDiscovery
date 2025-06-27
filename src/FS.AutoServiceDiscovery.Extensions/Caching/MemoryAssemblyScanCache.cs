using System.Collections.Concurrent;
using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Caching;

/// <summary>
/// In-memory implementation of assembly scan cache with automatic invalidation based on assembly metadata.
/// This implementation uses ConcurrentDictionary for thread-safety and includes smart invalidation logic
/// to ensure cache coherency when assemblies are updated or reloaded.
/// </summary>
public class MemoryAssemblyScanCache : IAssemblyScanCache
{
    // ConcurrentDictionary ensures thread-safety for multi-threaded scenarios like ASP.NET applications
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    // Bu statistics'i field olarak tanımlıyoruz ki ref sorunları olmasın
    private long _totalRequests = 0;
    private long _cacheHits = 0;
    private long _cacheMisses = 0;

    /// <summary>
    /// Represents a single cache entry with the scan results and metadata for invalidation.
    /// </summary>
    private class CacheEntry
    {
        public IEnumerable<ServiceRegistrationInfo> Results { get; set; } = null!;
        public DateTime LastWriteTime { get; set; }
        public long AssemblySize { get; set; }
        public DateTime CachedAt { get; set; }
    }

    /// <summary>
    /// Attempts to retrieve previously cached scan results for a specific assembly.
    /// This method implements the core "lookup" functionality of our caching system.
    /// 
    /// The method works like checking if you already have a book summary on your desk before
    /// going to read the entire book again. It first generates a unique identifier for the
    /// assembly (like a library book's ISBN), then checks if we have already processed this
    /// assembly before and if that cached information is still valid.
    /// 
    /// The cache validation is crucial here - we need to ensure that the assembly hasn't
    /// changed since we last scanned it. This is similar to checking if a book has been
    /// revised since you last read it. We do this by comparing file timestamps and sizes.
    /// </summary>
    /// <param name="assembly">The assembly to get cached results for. This serves as our "search key"</param>
    /// <param name="cachedResults">
    /// The cached results if found and still valid, null otherwise. This is an "out" parameter
    /// because we need to return both a success indicator (the return value) and the actual
    /// data (this parameter) if successful. Think of it like a library checkout system that
    /// tells you both whether the book is available AND hands you the book if it is.
    /// </param>
    /// <returns>
    /// True if cached results exist and are still valid (meaning the assembly hasn't changed
    /// since we last scanned it), false otherwise. This boolean acts as our "confidence indicator"
    /// - we're telling the caller whether they can trust the cached data or need to perform a fresh scan.
    /// </returns>
    public bool TryGetCachedResults(Assembly assembly, out IEnumerable<ServiceRegistrationInfo>? cachedResults)
    {
        cachedResults = null;

        // Increment total requests for statistics tracking - artık ref kullanmıyoruz
        Interlocked.Increment(ref _totalRequests);

        var cacheKey = GenerateCacheKey(assembly);

        if (!_cache.TryGetValue(cacheKey, out var entry))
        {
            Interlocked.Increment(ref _cacheMisses);
            return false;
        }

        // Validate cache entry against current assembly metadata
        // This is crucial for cache coherency - we need to ensure the cached data
        // corresponds to the current version of the assembly
        if (!IsEntryValid(assembly, entry))
        {
            // Remove invalid entry and count as cache miss
            _cache.TryRemove(cacheKey, out _);
            Interlocked.Increment(ref _cacheMisses);
            return false;
        }

        cachedResults = entry.Results;
        Interlocked.Increment(ref _cacheHits);
        return true;
    }
    
    /// <summary>
    /// Stores the results of an assembly scan in the cache for future retrieval.
    /// This method implements the "storage" functionality of our caching system.
    /// 
    /// Think of this as the process of creating a detailed index card after you've read a book.
    /// You write down all the important information (the service registrations we discovered)
    /// along with metadata about the book itself (assembly file size, last modified time) so
    /// you can later verify if the book has changed.
    /// 
    /// The implementation should be smart about memory management. In a long-running application,
    /// we don't want our cache to grow indefinitely, so implementations might include strategies
    /// like LRU (Least Recently Used) eviction or size limits. It's like having a finite desk
    /// space for your book summaries - sometimes you need to remove old ones to make room for new ones.
    /// </summary>
    /// <param name="assembly">
    /// The assembly that was scanned. This becomes our cache "key" - the identifier we'll use
    /// to look up this information later.
    /// </param>
    /// <param name="results">
    /// The service registration results discovered during the scan. This is our cache "value" -
    /// the expensive-to-compute information we want to avoid recalculating.
    /// </param>
    public void CacheResults(Assembly assembly, IEnumerable<ServiceRegistrationInfo> results)
    {
        var cacheKey = GenerateCacheKey(assembly);
        var resultsList = results.ToList(); // Materialize to avoid multiple enumeration

        var entry = new CacheEntry
        {
            Results = resultsList,
            LastWriteTime = GetAssemblyLastWriteTime(assembly),
            AssemblySize = GetAssemblySize(assembly),
            CachedAt = DateTime.UtcNow
        };

        _cache.AddOrUpdate(cacheKey, entry, (key, oldEntry) => entry);
    }
    
    /// <summary>
    /// Removes all cached results from the cache, effectively resetting it to an empty state.
    /// This method provides a "fresh start" mechanism for the caching system.
    /// 
    /// This operation is particularly valuable in several scenarios:
    /// 1. During unit testing, where you want each test to start with a clean slate
    /// 2. In development environments where assemblies might be frequently rebuilt
    /// 3. When you suspect cache corruption or inconsistency
    /// 4. During application shutdown to free memory resources
    /// 
    /// Think of this as clearing your entire desk of book summaries and starting fresh.
    /// Sometimes it's better to start over than to try to figure out which summaries
    /// might be outdated.
    /// 
    /// Implementation note: This method should be thread-safe, as multiple threads might
    /// be accessing the cache simultaneously in a web application environment.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();

        // Reset statistics - artık ref kullanmadığımız için basit assignment
        _totalRequests = 0;
        _cacheHits = 0;
        _cacheMisses = 0;
    }
    
    /// <summary>
    /// Retrieves performance metrics and statistical information about the cache's operation.
    /// This method provides visibility into how well our caching strategy is working.
    /// 
    /// Statistics are crucial for understanding cache effectiveness. Just like a librarian
    /// might track how often people request books that are already on the "recently returned"
    /// shelf versus books that need to be retrieved from deep storage, we track:
    /// 
    /// - Hit ratio: What percentage of lookups found cached data? Higher is better.
    /// - Total requests: How heavily is the cache being used?
    /// - Cache size: How much memory are we using for cached data?
    /// 
    /// These metrics help developers:
    /// 1. Validate that caching is actually improving performance
    /// 2. Identify if cache size limits need adjustment
    /// 3. Detect potential issues like cache thrashing (constant invalidation)
    /// 4. Optimize cache policies based on actual usage patterns
    /// 
    /// In production systems, these statistics might be exported to monitoring systems
    /// like Application Insights or Prometheus for ongoing performance tracking.
    /// </summary>
    /// <returns>
    /// A snapshot of current cache performance metrics. The returned object should represent
    /// the cache state at the moment this method is called, not a live-updating reference.
    /// This prevents race conditions where statistics might change while being read.
    /// </returns>
    public CacheStatistics GetStatistics()
    {
        // Return a snapshot of current statistics - thread-safe read
        return new CacheStatistics
        {
            TotalRequests = Interlocked.Read(ref _totalRequests),
            CacheHits = Interlocked.Read(ref _cacheHits),
            CacheMisses = Interlocked.Read(ref _cacheMisses),
            CachedAssembliesCount = _cache.Count,
            TotalCachedServices = _cache.Values.Sum(e => e.Results.Count())
        };
    }

    /// <summary>
    /// Generates a unique cache key for an assembly based on its full name.
    /// The full name includes version information, which helps with cache invalidation
    /// when assemblies are updated.
    /// </summary>
    private static string GenerateCacheKey(Assembly assembly)
    {
        // Using FullName ensures we differentiate between different versions of the same assembly
        return assembly.FullName ?? assembly.GetName().Name ?? assembly.ToString();
    }

    /// <summary>
    /// Validates whether a cache entry is still valid for the current assembly.
    /// This method implements our cache invalidation strategy based on assembly metadata.
    /// </summary>
    private static bool IsEntryValid(Assembly assembly, CacheEntry entry)
    {
        try
        {
            // Check if assembly's last write time has changed
            var currentLastWriteTime = GetAssemblyLastWriteTime(assembly);
            if (currentLastWriteTime != entry.LastWriteTime)
                return false;

            // Additional validation: check assembly size as a secondary verification
            var currentSize = GetAssemblySize(assembly);
            return currentSize == entry.AssemblySize;
        }
        catch
        {
            // If we can't validate, assume invalid to be safe
            return false;
        }
    }

    /// <summary>
    /// Gets the last write time of an assembly file for cache invalidation purposes.
    /// This is a key component of our invalidation strategy.
    /// </summary>
    private static DateTime GetAssemblyLastWriteTime(Assembly assembly)
    {
        try
        {
            if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
            {
                // For dynamic assemblies, use a constant time to avoid cache invalidation
                return DateTime.MinValue;
            }

            return File.GetLastWriteTimeUtc(assembly.Location);
        }
        catch
        {
            // Fallback for assemblies we can't get file info for
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Gets the size of an assembly file as an additional validation metric.
    /// </summary>
    private static long GetAssemblySize(Assembly assembly)
    {
        try
        {
            if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
                return 0;

            var fileInfo = new FileInfo(assembly.Location);
            return fileInfo.Exists ? fileInfo.Length : 0;
        }
        catch
        {
            return 0;
        }
    }
}