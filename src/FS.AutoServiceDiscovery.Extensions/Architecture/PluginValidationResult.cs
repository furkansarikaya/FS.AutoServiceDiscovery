namespace FS.AutoServiceDiscovery.Extensions.Architecture;

/// <summary>
/// Represents the result of a plugin validation operation, including success status and detailed information
/// about any issues discovered during the validation process.
/// 
/// This class serves as a communication mechanism between plugins and the core discovery system,
/// providing structured feedback about the health and validity of discovered services.
/// 
/// Think of this class as a detailed medical report after a health checkup - it tells you not just
/// whether everything is okay, but specifically what was checked, what issues were found,
/// and what actions might be needed to address any problems.
/// </summary>
public class PluginValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation completed successfully without finding any critical issues.
    /// 
    /// This property represents the overall health status of the validation. However, a successful
    /// validation doesn't necessarily mean no issues were found - it means no critical issues
    /// that would prevent the system from functioning were discovered.
    /// 
    /// The distinction between success and perfection is important:
    /// - Success = system can function, services can be registered
    /// - Perfection = no warnings, no potential optimizations, no minor issues
    /// 
    /// This allows the system to proceed with registration even when there are minor issues
    /// that users should be aware of but that don't prevent normal operation.
    /// </summary>
    public bool IsValid { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a collection of error messages describing critical issues that prevent
    /// the discovered services from being registered successfully.
    /// 
    /// Errors represent serious problems that would cause runtime failures:
    /// - Circular dependencies that can't be resolved
    /// - Missing required dependencies
    /// - Incompatible service lifetimes that would cause scope violations
    /// - Services that conflict in ways that can't be automatically resolved
    /// 
    /// When errors are present, IsValid should be false, and the registration process
    /// should be halted for the affected services to prevent runtime failures.
    /// 
    /// Error messages should be:
    /// - Specific enough for developers to understand the problem
    /// - Actionable, providing guidance on how to fix the issue
    /// - Context-aware, including relevant type names and assembly information
    /// </summary>
    public List<string> Errors { get; set; } = new();
    
    /// <summary>
    /// Gets or sets a collection of warning messages describing potential issues or
    /// suboptimal configurations that don't prevent registration but might cause problems.
    /// 
    /// Warnings alert users to situations that might not be ideal but won't break the system:
    /// - Performance concerns (e.g., expensive singleton services)
    /// - Potential conflicts (e.g., multiple implementations without clear precedence)
    /// - Best practice violations (e.g., services with inappropriate lifetimes)
    /// - Missing optional dependencies that might affect functionality
    /// 
    /// Warnings allow the system to proceed while giving users visibility into potential
    /// improvements or areas that might need attention in the future.
    /// 
    /// Warning messages should:
    /// - Explain why something might be problematic
    /// - Suggest specific improvements when possible
    /// - Indicate the potential impact of ignoring the warning
    /// </summary>
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>
    /// Gets or sets a collection of informational messages providing details about
    /// the validation process and any optimizations or modifications that were applied.
    /// 
    /// Information messages help users understand what happened during validation:
    /// - Services that were automatically optimized or reordered
    /// - Duplicates that were removed or consolidated
    /// - Assumptions that were made about ambiguous configurations
    /// - Statistics about the validation process (e.g., number of services processed)
    /// 
    /// These messages are particularly valuable for:
    /// - Debugging unexpected behavior
    /// - Understanding the plugin's decision-making process
    /// - Monitoring the health of the discovery system over time
    /// - Learning how to better configure services for optimal results
    /// </summary>
    public List<string> Information { get; set; } = new();
    
    /// <summary>
    /// Creates a successful validation result with no issues.
    /// This is a convenience method for the common case where validation passes without problems.
    /// </summary>
    /// <returns>A validation result indicating successful validation with no issues found.</returns>
    public static PluginValidationResult Success() => new();
    
    /// <summary>
    /// Creates a failed validation result with the specified error message.
    /// This is a convenience method for quickly creating validation failures.
    /// </summary>
    /// <param name="errorMessage">The error message describing why validation failed.</param>
    /// <returns>A validation result indicating failure with the specified error.</returns>
    public static PluginValidationResult Failure(string errorMessage) => new()
    {
        IsValid = false,
        Errors = { errorMessage }
    };
    
    /// <summary>
    /// Creates a successful validation result with warning messages.
    /// This allows for successful validation while alerting users to potential issues.
    /// </summary>
    /// <param name="warningMessages">Warning messages to include in the result.</param>
    /// <returns>A validation result indicating success but with warnings.</returns>
    public static PluginValidationResult SuccessWithWarnings(params string[] warningMessages) => new()
    {
        Warnings = warningMessages.ToList()
    };
    
    /// <summary>
    /// Adds an error message to this validation result and marks it as invalid.
    /// This method provides a fluent interface for building up validation results.
    /// </summary>
    /// <param name="message">The error message to add.</param>
    /// <returns>This validation result for method chaining.</returns>
    public PluginValidationResult AddError(string message)
    {
        IsValid = false;
        Errors.Add(message);
        return this;
    }
    
    /// <summary>
    /// Adds a warning message to this validation result.
    /// Warnings don't affect the validity status but provide important feedback to users.
    /// </summary>
    /// <param name="message">The warning message to add.</param>
    /// <returns>This validation result for method chaining.</returns>
    public PluginValidationResult AddWarning(string message)
    {
        Warnings.Add(message);
        return this;
    }
    
    /// <summary>
    /// Adds an informational message to this validation result.
    /// Information messages provide context and details about the validation process.
    /// </summary>
    /// <param name="message">The informational message to add.</param>
    /// <returns>This validation result for method chaining.</returns>
    public PluginValidationResult AddInformation(string message)
    {
        Information.Add(message);
        return this;
    }
    
    /// <summary>
    /// Gets a value indicating whether this validation result has any messages (errors, warnings, or information).
    /// This property helps determine whether there's anything meaningful to report to users.
    /// </summary>
    public bool HasMessages => Errors.Any() || Warnings.Any() || Information.Any();
    
    /// <summary>
    /// Gets the total number of issues (errors + warnings) found during validation.
    /// This provides a quick way to assess the overall health of the validation results.
    /// </summary>
    public int TotalIssueCount => Errors.Count + Warnings.Count;
}