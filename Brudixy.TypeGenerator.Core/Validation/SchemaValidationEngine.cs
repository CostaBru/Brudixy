using System;
using System.Collections.Generic;
using System.Linq;

namespace Brudixy.TypeGenerator.Core.Validation
{
    /// <summary>
    /// Core validation orchestrator that executes validation rules and collects results.
    /// </summary>
    public class SchemaValidationEngine
    {
        private readonly List<IValidationRule> _rules;
        private bool _rulesOrdered;

        public SchemaValidationEngine()
        {
            _rules = new List<IValidationRule>();
            _rulesOrdered = false;
        }

        /// <summary>
        /// Registers a validation rule with the engine.
        /// </summary>
        /// <param name="rule">The validation rule to register.</param>
        public void RegisterRule(IValidationRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            _rules.Add(rule);
            _rulesOrdered = false; // Mark that rules need to be reordered
        }

        /// <summary>
        /// Registers multiple validation rules with the engine.
        /// </summary>
        /// <param name="rules">The validation rules to register.</param>
        public void RegisterRules(IEnumerable<IValidationRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            foreach (var rule in rules)
            {
                if (rule != null)
                {
                    _rules.Add(rule);
                }
            }

            _rulesOrdered = false;
        }

        /// <summary>
        /// Validates the schema using all registered rules.
        /// </summary>
        /// <param name="context">The validation context.</param>
        /// <returns>The validation result containing all errors and warnings.</returns>
        public ValidationResult Validate(ValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var result = new ValidationResult();

            // Ensure rules are ordered by priority
            EnsureRulesOrdered();

            // Execute each rule and collect errors/warnings
            foreach (var rule in _rules)
            {
                try
                {
                    rule.Validate(context, result);
                }
                catch (Exception ex)
                {
                    // Convert rule exceptions to validation errors
                    result.AddError(new ValidationError(
                        context.FilePath,
                        $"ValidationRule.{rule.RuleName}",
                        $"Validation rule '{rule.RuleName}' threw an exception: {ex.Message}",
                        "This is an internal error. Please report this issue."));
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the count of registered validation rules.
        /// </summary>
        public int RuleCount => _rules.Count;

        /// <summary>
        /// Gets the names of all registered validation rules in execution order.
        /// </summary>
        public IReadOnlyList<string> GetRuleNames()
        {
            EnsureRulesOrdered();
            return _rules.Select(r => r.RuleName).ToList();
        }

        /// <summary>
        /// Ensures rules are ordered by priority for deterministic execution.
        /// </summary>
        private void EnsureRulesOrdered()
        {
            if (!_rulesOrdered)
            {
                // Sort by priority (ascending), then by name for deterministic ordering
                _rules.Sort((a, b) =>
                {
                    var priorityComparison = a.Priority.CompareTo(b.Priority);
                    if (priorityComparison != 0)
                    {
                        return priorityComparison;
                    }

                    return string.Compare(a.RuleName, b.RuleName, StringComparison.Ordinal);
                });

                _rulesOrdered = true;
            }
        }

        /// <summary>
        /// Creates a default validation engine with all standard validation rules registered.
        /// </summary>
        public static SchemaValidationEngine CreateDefault()
        {
            var engine = new SchemaValidationEngine();

            // Register all standard validation rules
            engine.RegisterRule(new Rules.ColumnTypeValidationRule());
            engine.RegisterRule(new Rules.ColumnReferenceValidationRule());
            engine.RegisterRule(new Rules.ConstraintValidationRule());
            engine.RegisterRule(new Rules.CodeGenerationOptionsValidationRule());
            engine.RegisterRule(new Rules.DatasetValidationRule());
            // Additional rules will be registered here as they are implemented

            return engine;
        }
    }
}
