using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;

namespace FS.AutoServiceDiscovery.Extensions.Attributes;

/// <summary>
/// Enhanced conditional service attribute that supports both string-based and expression-based conditions.
/// 
/// This attribute enables sophisticated conditional service registration using either simple key-value
/// configuration checks or complex expression-based logic that provides compile-time safety and
/// IntelliSense support.
/// 
/// The expression-based approach transforms service registration from simple configuration lookups
/// into a powerful domain-specific language that can express complex business rules and environmental
/// conditions in a type-safe, readable manner.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ConditionalServiceAttribute : Attribute
{
    /// <summary>
    /// Gets the configuration key to check for simple key-value conditions.
    /// This property is used for backward compatibility with the existing string-based system.
    /// </summary>
    public string? ConfigurationKey { get; }
    
    /// <summary>
    /// Gets the expected value for simple key-value conditions.
    /// This property is used for backward compatibility with the existing string-based system.
    /// </summary>
    public string? ExpectedValue { get; }
    
    /// <summary>
    /// Gets the compiled expression that defines the conditional logic.
    /// This expression provides access to a rich context object with environment information,
    /// configuration values, and helper methods for complex conditional logic.
    /// </summary>
    public Func<IConditionalContext, bool>? ConditionExpression { get; }
    
    /// <summary>
    /// Gets whether this condition uses the new expression-based evaluation system.
    /// This flag helps the discovery system determine which evaluation method to use.
    /// </summary>
    public bool IsExpressionBased { get; }

    /// <summary>
    /// Creates a simple string-based conditional service attribute for backward compatibility.
    /// This constructor maintains compatibility with existing code while providing a migration
    /// path to the more powerful expression-based system.
    /// </summary>
    /// <param name="configurationKey">The configuration key to check</param>
    /// <param name="expectedValue">The expected value for the configuration key</param>
    /// <example>
    /// <code>
    /// [ConditionalService("FeatureFlags:EnableEmailService", "true")]
    /// public class EmailService : IEmailService { }
    /// </code>
    /// </example>
    public ConditionalServiceAttribute(string configurationKey, string expectedValue)
    {
        ConfigurationKey = configurationKey ?? throw new ArgumentNullException(nameof(configurationKey));
        ExpectedValue = expectedValue ?? throw new ArgumentNullException(nameof(expectedValue));
        IsExpressionBased = false;
    }
    
    /// <summary>
    /// Creates an expression-based conditional service attribute using a lambda expression.
    /// 
    /// This constructor enables the powerful new DSL functionality that allows complex
    /// conditional logic to be expressed in a type-safe, IntelliSense-enabled manner.
    /// The expression receives a context object that provides access to environment
    /// information, configuration values, and utility methods.
    /// 
    /// The lambda expression approach provides several key benefits:
    /// - Compile-time type safety ensures conditions are valid
    /// - IntelliSense support makes writing conditions easier and less error-prone
    /// - Complex logic can be expressed naturally using C# operators
    /// - The context object provides a rich API for common conditional patterns
    /// </summary>
    /// <param name="conditionExpression">
    /// A lambda expression that takes an IConditionalContext and returns a boolean indicating
    /// whether the service should be registered. The context provides access to environment
    /// variables, configuration values, feature flags, and other runtime information.
    /// </param>
    /// <example>
    /// Simple environment check:
    /// <code>
    /// [ConditionalService(ctx => ctx.Environment.IsProduction())]
    /// public class ProductionEmailService : IEmailService { }
    /// </code>
    /// 
    /// Complex conditional logic:
    /// <code>
    /// [ConditionalService(ctx => 
    ///     ctx.Environment.IsProduction() &amp;&amp; 
    ///     ctx.FeatureEnabled("NewAuth") &amp;&amp; 
    ///     !ctx.Configuration.GetValue&lt;bool&gt;("MaintenanceMode"))]
    /// public class AdvancedAuthService : IAuthService { }
    /// </code>
    /// 
    /// Multiple conditions with different priorities:
    /// <code>
    /// [ConditionalService(ctx => ctx.Environment.IsDevelopment())]
    /// [ConditionalService(ctx => ctx.FeatureEnabled("BetaFeatures"))]
    /// public class BetaService : IBetaService { }
    /// </code>
    /// </example>
    public ConditionalServiceAttribute(Expression<Func<IConditionalContext, bool>> conditionExpression)
    {
        if (conditionExpression == null)
            throw new ArgumentNullException(nameof(conditionExpression));
            
        // Compile the expression for efficient runtime evaluation
        // The compilation happens once during attribute construction, providing
        // optimal performance during service discovery
        ConditionExpression = conditionExpression.Compile();
        IsExpressionBased = true;
    }

    /// <summary>
    /// Evaluates this conditional attribute against the provided context to determine
    /// if the associated service should be registered.
    /// 
    /// This method abstracts the difference between string-based and expression-based
    /// conditions, providing a unified evaluation interface for the discovery system.
    /// </summary>
    /// <param name="context">
    /// The conditional context containing environment information, configuration,
    /// and other data needed for condition evaluation.
    /// </param>
    /// <returns>
    /// True if the condition is satisfied and the service should be registered,
    /// false otherwise.
    /// </returns>
    public bool EvaluateCondition(IConditionalContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (IsExpressionBased)
        {
            // Use the compiled expression for evaluation
            // This provides maximum flexibility and performance for complex conditions
            return ConditionExpression?.Invoke(context) ?? false;
        }
        else
        {
            // Fall back to simple string-based evaluation for backward compatibility
            // This ensures existing code continues to work without modification
            var configValue = context.Configuration?.GetValue<string>(ConfigurationKey ?? string.Empty);
            return string.Equals(configValue, ExpectedValue, StringComparison.OrdinalIgnoreCase);
        }
    }
}