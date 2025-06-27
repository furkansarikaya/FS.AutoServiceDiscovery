using System.Reflection;
using FS.AutoServiceDiscovery.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Caching;

/// <summary>
/// Defines a contract for caching assembly scan results to improve performance on subsequent scans.
/// This is particularly valuable in scenarios where the same assemblies are scanned multiple times,
/// such as in unit tests or applications with frequent restarts.
/// 
/// Think of this interface as a blueprint for a smart storage system that remembers what it has
/// already discovered, much like how a librarian might keep a card catalog of books they've
/// already categorized to avoid re-cataloging them repeatedly.
/// </summary>
public interface IAssemblyScanCache
{
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
    bool TryGetCachedResults(Assembly assembly, out IEnumerable<ServiceRegistrationInfo>? cachedResults);
    
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
    void CacheResults(Assembly assembly, IEnumerable<ServiceRegistrationInfo> results);
    
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
    void ClearCache();
    
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
    CacheStatistics GetStatistics();
}