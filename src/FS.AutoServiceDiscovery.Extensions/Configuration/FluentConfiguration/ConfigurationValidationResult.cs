namespace FS.AutoServiceDiscovery.Extensions.Configuration.FluentConfiguration;

/// <summary>
/// Represents the result of a configuration validation operation, providing detailed information
/// about any issues found during the validation process.
/// 
/// This class serves as a comprehensive report card for configuration validation. Just like a
/// code review provides both approval status and detailed feedback about potential improvements,
/// this result class tells you whether the configuration is valid and provides specific guidance
/// about any issues that need attention.
/// 
/// The validation result supports multiple severity levels (errors, warnings, information) to
/// provide nuanced feedback. This allows the system to distinguish between configuration problems
/// that must be fixed (errors) and potential improvements that should be considered (warnings).
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the configuration validation passed successfully.
    /// 
    /// A validation is considered successful if no critical errors were found that would prevent
    /// the system from functioning correctly. Warnings and informational messages don't affect
    /// the success status, allowing the system to proceed with non-critical issues present.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// Gets the collection of error messages describing critical configuration issues.
    /// 
    /// Errors represent serious configuration problems that would cause the service discovery
    /// system to malfunction or fail completely. When errors are present, IsValid should be
    /// false and the configuration should be corrected before proceeding.
    /// 
    /// Examples of errors include:
    /// - Required assemblies that cannot be loaded
    /// - Circular dependencies in service configuration
    /// - Invalid or conflicting lifetime specifications
    /// - Missing required configuration values for conditional services
    /// </summary>
    public List<string> Errors { get; } = [];

    /// <summary>
    /// Gets the collection of warning messages describing potential configuration issues.
    /// 
    /// Warnings represent configuration choices that might not be optimal but don't prevent
    /// the system from functioning. They alert developers to potential performance issues,
    /// best practice violations, or configuration patterns that might cause problems in
    /// certain scenarios.
    /// 
    /// Examples of warnings include:
    /// - Performance concerns with current configuration choices
    /// - Deprecated configuration patterns
    /// - Potential conflicts between different configuration options
    /// - Missing optional dependencies that might affect functionality
    /// </summary>
    public List<string> Warnings { get; } = [];

    /// <summary>
    /// Gets the collection of informational messages providing guidance and suggestions.
    /// 
    /// Information messages provide helpful guidance about the configuration without indicating
    /// problems. They might include optimization suggestions, usage tips, or explanations of
    /// how the current configuration will behave at runtime.
    /// 
    /// Examples of information messages include:
    /// - Summary of which assemblies will be scanned
    /// - Explanation of how profile-based registration will work
    /// - Performance optimization suggestions
    /// - Configuration completeness reports
    /// </summary>
    public List<string> Information { get; } = [];

    /// <summary>
    /// Gets or sets the name of the validation rule that produced this result.
    /// 
    /// This property helps identify which validation rule generated the result, which is
    /// particularly useful when multiple validation rules are applied and you need to
    /// understand the source of specific messages.
    /// </summary>
    public string? ValidationRuleName { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the validation process.
    /// 
    /// This dictionary can contain any additional context information that might be useful
    /// for understanding the validation result, such as timing information, configuration
    /// paths that were checked, or statistical data about the validation process.
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

    /// <summary>
    /// Creates a successful validation result with no issues found.
    /// This is a convenience method for the common case where validation passes completely.
    /// </summary>
    /// <param name="ruleName">Optional name of the validation rule that passed.</param>
    /// <returns>A validation result indicating successful validation with no issues.</returns>
    public static ConfigurationValidationResult Success(string? ruleName = null)
    {
        return new ConfigurationValidationResult
        {
            ValidationRuleName = ruleName
        };
    }

    /// <summary>
    /// Creates a failed validation result with the specified error message.
    /// This is a convenience method for quickly creating validation failures.
    /// </summary>
    /// <param name="errorMessage">The error message describing why validation failed.</param>
    /// <param name="ruleName">Optional name of the validation rule that failed.</param>
    /// <returns>A validation result indicating failure with the specified error.</returns>
    public static ConfigurationValidationResult Failure(string errorMessage, string? ruleName = null)
    {
        return new ConfigurationValidationResult
        {
            IsValid = false,
            ValidationRuleName = ruleName
        }.AddError(errorMessage);
    }

    /// <summary>
    /// Creates a successful validation result with warning messages.
    /// This allows for successful validation while alerting users to potential issues.
    /// </summary>
    /// <param name="warningMessages">Warning messages to include in the result.</param>
    /// <param name="ruleName">Optional name of the validation rule.</param>
    /// <returns>A validation result indicating success but with warnings.</returns>
    public static ConfigurationValidationResult SuccessWithWarnings(string[] warningMessages, string? ruleName = null)
    {
        var result = new ConfigurationValidationResult
        {
            ValidationRuleName = ruleName
        };

        foreach (var warning in warningMessages)
        {
            result.Warnings.Add(warning);
        }

        return result;
    }

    /// <summary>
    /// Adds an error message to this validation result and marks it as invalid.
    /// This method provides a fluent interface for building up validation results.
    /// </summary>
    /// <param name="message">The error message to add.</param>
    /// <returns>This validation result for method chaining.</returns>
    public ConfigurationValidationResult AddError(string message)
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
    public ConfigurationValidationResult AddWarning(string message)
    {
        Warnings.Add(message);
        return this;
    }

    /// <summary>
    /// Adds an informational message to this validation result.
    /// Information messages provide context and guidance about the validation process.
    /// </summary>
    /// <param name="message">The informational message to add.</param>
    /// <returns>This validation result for method chaining.</returns>
    public ConfigurationValidationResult AddInformation(string message)
    {
        Information.Add(message);
        return this;
    }

    /// <summary>
    /// Adds metadata to this validation result.
    /// Metadata provides additional context that might be useful for debugging or reporting.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>This validation result for method chaining.</returns>
    public ConfigurationValidationResult AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Merges another validation result into this one, combining all messages and metadata.
    /// This method is useful when multiple validation rules need to be combined into a single result.
    /// </summary>
    /// <param name="other">The other validation result to merge.</param>
    /// <returns>This validation result for method chaining.</returns>
    public ConfigurationValidationResult Merge(ConfigurationValidationResult other)
    {
        if (other == null) return this;

        // If either result is invalid, the merged result is invalid
        if (!other.IsValid)
            IsValid = false;

        // Merge all messages
        Errors.AddRange(other.Errors);
        Warnings.AddRange(other.Warnings);
        Information.AddRange(other.Information);

        // Merge metadata (existing keys are overwritten)
        foreach (var kvp in other.Metadata)
        {
            Metadata[kvp.Key] = kvp.Value;
        }

        return this;
    }

    /// <summary>
    /// Gets a value indicating whether this validation result has any messages (errors, warnings, or information).
    /// This property helps determine whether there's anything meaningful to report to users.
    /// </summary>
    public bool HasMessages => Errors.Count != 0 || Warnings.Count != 0 || Information.Count != 0;

    /// <summary>
    /// Gets the total number of issues (errors + warnings) found during validation.
    /// This provides a quick way to assess the overall health of the validation results.
    /// </summary>
    public int TotalIssueCount => Errors.Count + Warnings.Count;

    /// <summary>
    /// Gets the severity level of this validation result based on the types of messages present.
    /// This property provides a quick way to understand the overall severity of the validation outcome.
    /// </summary>
    public ValidationSeverity Severity
    {
        get
        {
            if (Errors.Count != 0) return ValidationSeverity.Error;
            if (Warnings.Count != 0) return ValidationSeverity.Warning;
            return Information.Count != 0 ? ValidationSeverity.Information : ValidationSeverity.Success;
        }
    }

    /// <summary>
    /// Generates a formatted text report of all validation messages for logging or display purposes.
    /// This method creates a human-readable summary that can be used for debugging or user feedback.
    /// </summary>
    /// <returns>A formatted string containing all validation messages organized by severity.</returns>
    public string GenerateReport()
    {
        var report = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(ValidationRuleName))
        {
            report.AppendLine($"Validation Rule: {ValidationRuleName}");
            report.AppendLine($"Overall Result: {(IsValid ? "VALID" : "INVALID")}");
            report.AppendLine();
        }

        if (Errors.Count != 0)
        {
            report.AppendLine("ERRORS:");
            foreach (var error in Errors)
            {
                report.AppendLine($"  - {error}");
            }
            report.AppendLine();
        }

        if (Warnings.Count != 0)
        {
            report.AppendLine("WARNINGS:");
            foreach (var warning in Warnings)
            {
                report.AppendLine($"  - {warning}");
            }
            report.AppendLine();
        }

        if (Information.Count != 0)
        {
            report.AppendLine("INFORMATION:");
            foreach (var info in Information)
            {
                report.AppendLine($"  - {info}");
            }
            report.AppendLine();
        }

        if (Metadata.Count == 0) 
            return report.ToString().Trim();
        report.AppendLine("METADATA:");
        foreach (var kvp in Metadata)
        {
            report.AppendLine($"  {kvp.Key}: {kvp.Value}");
        }

        return report.ToString().Trim();
    }

    /// <summary>
    /// Creates a string representation of this validation result for debugging purposes.
    /// </summary>
    /// <returns>A concise string describing the validation result status and message counts.</returns>
    public override string ToString()
    {
        var status = IsValid ? "Valid" : "Invalid";
        var messageCounts = new List<string>();

        if (Errors.Count != 0) messageCounts.Add($"{Errors.Count} error(s)");
        if (Warnings.Count != 0) messageCounts.Add($"{Warnings.Count} warning(s)");
        if (Information.Count != 0) messageCounts.Add($"{Information.Count} info");

        var details = messageCounts.Count != 0 ? $" [{string.Join(", ", messageCounts)}]" : "";
        var ruleName = !string.IsNullOrEmpty(ValidationRuleName) ? $" ({ValidationRuleName})" : "";

        return $"ConfigurationValidationResult: {status}{details}{ruleName}";
    }
}

/// <summary>
/// Defines the severity levels for validation results.
/// This enumeration helps categorize validation outcomes and determine appropriate responses.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Validation completed successfully with no issues found.
    /// </summary>
    Success,

    /// <summary>
    /// Validation completed with informational messages but no problems.
    /// </summary>
    Information,

    /// <summary>
    /// Validation completed with warnings about potential issues.
    /// </summary>
    Warning,

    /// <summary>
    /// Validation failed with critical errors that must be addressed.
    /// </summary>
    Error
}