using System.Collections.Generic;
using System.Linq;

namespace Brudixy.TypeGenerator.Core.Validation
{
    /// <summary>
    /// Represents the outcome of schema validation with errors and warnings.
    /// </summary>
    public class ValidationResult
    {
        private readonly List<ValidationError> _errors;
        private readonly List<ValidationWarning> _warnings;

        public ValidationResult()
        {
            _errors = new List<ValidationError>();
            _warnings = new List<ValidationWarning>();
        }

        /// <summary>
        /// Gets whether the validation passed (no errors).
        /// Warnings do not affect validity.
        /// </summary>
        public bool IsValid => _errors.Count == 0;

        /// <summary>
        /// Gets the list of validation errors.
        /// </summary>
        public IReadOnlyList<ValidationError> Errors => _errors;

        /// <summary>
        /// Gets the list of validation warnings.
        /// </summary>
        public IReadOnlyList<ValidationWarning> Warnings => _warnings;

        /// <summary>
        /// Adds a validation error to the result.
        /// </summary>
        public void AddError(ValidationError error)
        {
            if (error != null)
            {
                _errors.Add(error);
            }
        }

        /// <summary>
        /// Adds a validation warning to the result.
        /// </summary>
        public void AddWarning(ValidationWarning warning)
        {
            if (warning != null)
            {
                _warnings.Add(warning);
            }
        }

        /// <summary>
        /// Gets a formatted summary of all errors and warnings.
        /// </summary>
        public string GetSummary()
        {
            var lines = new List<string>();

            if (_errors.Any())
            {
                lines.Add($"Validation failed with {_errors.Count} error(s):");
                foreach (var error in _errors)
                {
                    lines.Add($"  - {error}");
                }
            }

            if (_warnings.Any())
            {
                lines.Add($"Validation completed with {_warnings.Count} warning(s):");
                foreach (var warning in _warnings)
                {
                    lines.Add($"  - {warning}");
                }
            }

            if (!_errors.Any() && !_warnings.Any())
            {
                lines.Add("Validation passed successfully.");
            }

            return string.Join("\n", lines);
        }
    }
}
