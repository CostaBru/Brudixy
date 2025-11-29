namespace Brudixy.TypeGenerator.Core.Validation
{
    /// <summary>
    /// Interface for schema validation rules.
    /// Each rule validates a specific aspect of the schema.
    /// </summary>
    public interface IValidationRule
    {
        /// <summary>
        /// Gets the unique name of this validation rule.
        /// </summary>
        string RuleName { get; }

        /// <summary>
        /// Gets the execution order priority (lower values execute first).
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Validates the schema and adds errors/warnings to the result.
        /// </summary>
        /// <param name="context">The validation context containing the table and file information.</param>
        /// <param name="result">The validation result to add errors and warnings to.</param>
        void Validate(ValidationContext context, ValidationResult result);
    }
}
