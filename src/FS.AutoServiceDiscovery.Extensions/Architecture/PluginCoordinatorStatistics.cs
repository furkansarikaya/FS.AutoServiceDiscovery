namespace FS.AutoServiceDiscovery.Extensions.Architecture;

/// <summary>
/// Contains comprehensive statistics about the plugin coordinator's operation and performance over time.
/// 
/// This class represents the "dashboard metrics" for the plugin coordination system. Just like a
/// factory manager needs to know production statistics (how many items produced, failure rates,
/// efficiency metrics), a plugin coordinator needs to track its operational metrics to ensure
/// optimal performance and identify potential issues.
/// 
/// These statistics serve multiple important purposes:
/// 1. **Performance Monitoring**: Understanding how well the plugin system is performing
/// 2. **Capacity Planning**: Determining if the current configuration can handle expected load
/// 3. **Problem Identification**: Spotting trends that might indicate emerging issues
/// 4. **Optimization Guidance**: Providing data-driven insights for system improvements
/// 5. **Reporting**: Enabling comprehensive reporting for stakeholders and monitoring systems
/// 
/// The statistics are designed to be both human-readable (for debugging and analysis) and
/// machine-readable (for automated monitoring and alerting systems).
/// </summary>
public class PluginCoordinatorStatistics
{
    /// <summary>
    /// Gets or sets the number of plugins currently registered with the coordinator.
    /// 
    /// This metric provides insight into the complexity of the plugin ecosystem. A sudden
    /// change in this number might indicate configuration changes or deployment issues.
    /// </summary>
    public int RegisteredPluginsCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of plugin coordination cycles that have been executed.
    /// 
    /// This represents the total "workload" that the coordinator has handled. Each execution
    /// cycle involves running all registered plugins against a set of assemblies.
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of coordination cycles that completed successfully without critical errors.
    /// 
    /// A successful execution means all plugins were able to run and validate their results,
    /// though individual plugins might have had warnings or non-critical issues.
    /// </summary>
    public long SuccessfulExecutions { get; set; }

    /// <summary>
    /// Gets or sets the total number of individual plugin executions across all coordination cycles.
    /// 
    /// This metric provides insight into the total computational work performed. If you have
    /// 5 plugins and run 10 coordination cycles, this number would be 50.
    /// </summary>
    public long TotalPluginsExecuted { get; set; }

    /// <summary>
    /// Gets or sets the total number of services discovered across all plugin executions.
    /// 
    /// This represents the "productivity" of the plugin system - how many services have been
    /// successfully discovered and made available for dependency injection.
    /// </summary>
    public long TotalServicesDiscovered { get; set; }

    /// <summary>
    /// Gets or sets the time when statistics collection began.
    /// This provides context for understanding the time period these statistics cover.
    /// </summary>
    public DateTime StatisticsStartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Calculates the success rate as a percentage of successful executions.
    /// 
    /// This derived metric provides a quick assessment of system reliability. A success rate
    /// significantly below 100% indicates potential issues that need investigation.
    /// </summary>
    public double SuccessRate => TotalExecutions > 0 
        ? (double)SuccessfulExecutions / TotalExecutions * 100 
        : 0;

    /// <summary>
    /// Calculates the average number of plugins executed per coordination cycle.
    /// 
    /// This metric helps understand the typical plugin load and can indicate if the
    /// coordinator is being used consistently across different execution contexts.
    /// </summary>
    public double AveragePluginsPerExecution => TotalExecutions > 0 
        ? (double)TotalPluginsExecuted / TotalExecutions 
        : 0;

    /// <summary>
    /// Calculates the average number of services discovered per coordination cycle.
    /// 
    /// This productivity metric helps understand the typical output of the discovery process
    /// and can be used to identify changes in application complexity over time.
    /// </summary>
    public double AverageServicesPerExecution => TotalExecutions > 0 
        ? (double)TotalServicesDiscovered / TotalExecutions 
        : 0;

    /// <summary>
    /// Gets the duration for which statistics have been collected.
    /// This provides context for interpreting rate-based metrics.
    /// </summary>
    public TimeSpan StatisticsCollectionDuration => DateTime.UtcNow - StatisticsStartTime;

    /// <summary>
    /// Creates a snapshot copy of these statistics for safe sharing and analysis.
    /// 
    /// This method is important because statistics objects are often mutable and used
    /// in multithreaded environments. Creating a snapshot ensures that consumers get
    /// a consistent view of the data at a specific point in time.
    /// </summary>
    /// <returns>An immutable snapshot of the current statistics.</returns>
    public PluginCoordinatorStatistics CreateSnapshot()
    {
        return new PluginCoordinatorStatistics
        {
            RegisteredPluginsCount = this.RegisteredPluginsCount,
            TotalExecutions = this.TotalExecutions,
            SuccessfulExecutions = this.SuccessfulExecutions,
            TotalPluginsExecuted = this.TotalPluginsExecuted,
            TotalServicesDiscovered = this.TotalServicesDiscovered,
            StatisticsStartTime = this.StatisticsStartTime
        };
    }
}