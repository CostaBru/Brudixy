using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Interfaces.Generators;
using Brudixy.TypeGenerator.Core;
using Brudixy.TypeGenerator.Core.Validation;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests.Validation
{
    /// <summary>
    /// Property-based tests for validation completeness.
    /// </summary>
    [TestFixture]
    public class ValidationCompletenessPropertyTests
    {
        // Feature: yaml-schema-validation, Property 1: Validation completeness
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(DataTableObjGenerators) })]
        public Property ValidationCompleteness_AllRulesExecuted(DataTableObj table, string filePath)
        {
            // Arrange
            var engine = new SchemaValidationEngine();
            var mockFileSystem = new MockFileSystemAccessor();
            
            // Register test rules that track execution
            var executedRules = new List<string>();
            var rule1 = new TrackingValidationRule("Rule1", 1, executedRules);
            var rule2 = new TrackingValidationRule("Rule2", 2, executedRules);
            var rule3 = new TrackingValidationRule("Rule3", 3, executedRules);
            
            engine.RegisterRule(rule1);
            engine.RegisterRule(rule2);
            engine.RegisterRule(rule3);
            
            var context = new ValidationContext(table, filePath ?? "test.yaml", mockFileSystem);
            
            // Act
            var result = engine.Validate(context);
            
            // Assert
            return (executedRules.Count == 3).Label("All rules executed")
                .And(() => executedRules.Contains("Rule1")).Label("Rule1 executed")
                .And(() => executedRules.Contains("Rule2")).Label("Rule2 executed")
                .And(() => executedRules.Contains("Rule3")).Label("Rule3 executed")
                .And(() => result != null).Label("Result is not null");
        }

        // Feature: yaml-schema-validation, Property 1: Validation completeness
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(DataTableObjGenerators) })]
        public Property ValidationCompleteness_ResultContainsAllErrors(DataTableObj table, string filePath)
        {
            // Arrange
            var engine = new SchemaValidationEngine();
            var mockFileSystem = new MockFileSystemAccessor();
            
            // Register rules that add errors
            engine.RegisterRule(new ErrorAddingRule("ErrorRule1", 1, "Error 1"));
            engine.RegisterRule(new ErrorAddingRule("ErrorRule2", 2, "Error 2"));
            engine.RegisterRule(new ErrorAddingRule("ErrorRule3", 3, "Error 3"));
            
            var context = new ValidationContext(table, filePath ?? "test.yaml", mockFileSystem);
            
            // Act
            var result = engine.Validate(context);
            
            // Assert
            return (result.Errors.Count == 3).Label("All errors collected")
                .And(() => result.Errors.Any(e => e.Message == "Error 1")).Label("Error 1 present")
                .And(() => result.Errors.Any(e => e.Message == "Error 2")).Label("Error 2 present")
                .And(() => result.Errors.Any(e => e.Message == "Error 3")).Label("Error 3 present")
                .And(() => !result.IsValid).Label("Result is invalid when errors exist");
        }

        // Feature: yaml-schema-validation, Property 1: Validation completeness
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(DataTableObjGenerators) })]
        public Property ValidationCompleteness_ResultContainsAllWarnings(DataTableObj table, string filePath)
        {
            // Arrange
            var engine = new SchemaValidationEngine();
            var mockFileSystem = new MockFileSystemAccessor();
            
            // Register rules that add warnings
            engine.RegisterRule(new WarningAddingRule("WarningRule1", 1, "Warning 1"));
            engine.RegisterRule(new WarningAddingRule("WarningRule2", 2, "Warning 2"));
            
            var context = new ValidationContext(table, filePath ?? "test.yaml", mockFileSystem);
            
            // Act
            var result = engine.Validate(context);
            
            // Assert
            return (result.Warnings.Count == 2).Label("All warnings collected")
                .And(() => result.Warnings.Any(w => w.Message == "Warning 1")).Label("Warning 1 present")
                .And(() => result.Warnings.Any(w => w.Message == "Warning 2")).Label("Warning 2 present")
                .And(() => result.IsValid).Label("Result is valid when only warnings exist");
        }

        #region Test Helper Classes

        private class TrackingValidationRule : IValidationRule
        {
            private readonly List<string> _executedRules;

            public TrackingValidationRule(string name, int priority, List<string> executedRules)
            {
                RuleName = name;
                Priority = priority;
                _executedRules = executedRules;
            }

            public string RuleName { get; }
            public int Priority { get; }

            public void Validate(ValidationContext context, ValidationResult result)
            {
                _executedRules.Add(RuleName);
            }
        }

        private class ErrorAddingRule : IValidationRule
        {
            private readonly string _errorMessage;

            public ErrorAddingRule(string name, int priority, string errorMessage)
            {
                RuleName = name;
                Priority = priority;
                _errorMessage = errorMessage;
            }

            public string RuleName { get; }
            public int Priority { get; }

            public void Validate(ValidationContext context, ValidationResult result)
            {
                result.AddError(new ValidationError(context.FilePath, _errorMessage));
            }
        }

        private class WarningAddingRule : IValidationRule
        {
            private readonly string _warningMessage;

            public WarningAddingRule(string name, int priority, string warningMessage)
            {
                RuleName = name;
                Priority = priority;
                _warningMessage = warningMessage;
            }

            public string RuleName { get; }
            public int Priority { get; }

            public void Validate(ValidationContext context, ValidationResult result)
            {
                result.AddWarning(new ValidationWarning(context.FilePath, _warningMessage));
            }
        }

        private class MockFileSystemAccessor : IFileSystemAccessor
        {
            public string GetFileContents(string path)
            {
                return string.Empty;
            }
        }

        #endregion
    }

    /// <summary>
    /// FsCheck generators for DataTableObj and related types.
    /// </summary>
    public static class DataTableObjGenerators
    {
        public static Arbitrary<DataTableObj> DataTableObjArbitrary()
        {
            return Arb.From(Gen.Fresh(() =>
            {
                var table = new DataTableObj
                {
                    Table = Gen.Sample(1, 1, Arb.Generate<NonEmptyString>()).First().Get,
                    CodeGenerationOptions = new TableCodeGenerationOptions
                    {
                        Namespace = "Test.Namespace",
                        Class = "TestTable",
                        Sealed = true
                    }
                };

                // Add some random columns
                var columnCount = Gen.Choose(0, 5).Sample(1, 1).First();
                for (int i = 0; i < columnCount; i++)
                {
                    var columnName = $"Column{i}";
                    table.Columns[columnName] = "String";
                }

                table.EnsureDefaults();
                return table;
            }));
        }
    }
}
