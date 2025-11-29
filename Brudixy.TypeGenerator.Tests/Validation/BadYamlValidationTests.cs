using System.Linq;
using Brudixy.Interfaces.Generators;
using Brudixy.TypeGenerator.Core;
using Brudixy.TypeGenerator.Core.Validation;
using Brudixy.TypeGenerator.Core.Validation.Rules;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests.Validation
{
    /// <summary>
    /// Tests for validating bad YAML schemas and ensuring proper error reporting.
    /// </summary>
    [TestFixture]
    public class BadYamlValidationTests
    {
        private SchemaValidationEngine _engine;
        private MockFileSystemAccessor _fileSystem;

        [SetUp]
        public void SetUp()
        {
            _engine = new SchemaValidationEngine();
            _fileSystem = new MockFileSystemAccessor();
            
            // Register the column type validation rule
            _engine.RegisterRule(new ColumnTypeValidationRule());
        }

        [Test]
        public void Validate_ConflictingNullabilityModifiers_ReportsError()
        {
            // Arrange - column with both ? and ! modifiers
            var table = new DataTableObj
            {
                Table = "TestTable",
                CodeGenerationOptions = new TableCodeGenerationOptions
                {
                    Namespace = "Test",
                    Class = "TestTable"
                }
            };
            
            table.Columns["BadColumn"] = "String?!"; // Both nullable and non-null
            // Don't call EnsureDefaults() - validation should happen on raw YAML data
            
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for conflicting modifiers");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("nullable") && e.Message.Contains("non-null")),
                "Error should mention conflicting nullable modifiers");
        }

        [Test]
        public void Validate_MultipleStructuralModifiers_ReportsError()
        {
            // Arrange - column with both array and range modifiers
            var table = new DataTableObj
            {
                Table = "TestTable",
                CodeGenerationOptions = new TableCodeGenerationOptions
                {
                    Namespace = "Test",
                    Class = "TestTable"
                }
            };
            
            table.Columns["BadColumn"] = "Int32[]<>"; // Both array and range
            // Don't call EnsureDefaults() - validation should happen on raw YAML data
            
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for multiple structural modifiers");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("structural modifier")),
                "Error should mention structural modifiers");
        }

        [Test]
        public void Validate_ConflictingAllowNullWithNullableType_ReportsError()
        {
            // Arrange - nullable type with AllowNull: false
            var table = new DataTableObj
            {
                Table = "TestTable",
                CodeGenerationOptions = new TableCodeGenerationOptions
                {
                    Namespace = "Test",
                    Class = "TestTable"
                }
            };
            
            table.Columns["BadColumn"] = "String?";
            table.ColumnOptions["BadColumn"] = new System.Collections.Generic.Dictionary<object, object>
            {
                { "AllowNull", false }
            };
            // Don't call EnsureDefaults() - validation should happen on raw YAML data
            
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for conflicting null settings");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.PropertyPath.Contains("BadColumn") && e.Message.Contains("conflicting")),
                "Error should mention conflicting null settings for BadColumn");
        }

        [Test]
        public void Validate_ConflictingAllowNullWithNonNullType_ReportsError()
        {
            // Arrange - non-null type with AllowNull: true
            var table = new DataTableObj
            {
                Table = "TestTable",
                CodeGenerationOptions = new TableCodeGenerationOptions
                {
                    Namespace = "Test",
                    Class = "TestTable"
                }
            };
            
            table.Columns["BadColumn"] = "String!";
            table.ColumnOptions["BadColumn"] = new System.Collections.Generic.Dictionary<object, object>
            {
                { "AllowNull", true }
            };
            // Don't call EnsureDefaults() - validation should happen on raw YAML data
            
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for conflicting null settings");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.PropertyPath.Contains("BadColumn") && e.Message.Contains("conflicting")),
                "Error should mention conflicting null settings for BadColumn");
        }

        [Test]
        public void Validate_EmptyEnumType_ReportsWarning()
        {
            // Arrange - column with empty EnumType
            var table = new DataTableObj
            {
                Table = "TestTable",
                CodeGenerationOptions = new TableCodeGenerationOptions
                {
                    Namespace = "Test",
                    Class = "TestTable"
                }
            };
            
            table.Columns["EnumColumn"] = "Int32";
            table.ColumnOptions["EnumColumn"] = new System.Collections.Generic.Dictionary<object, object>
            {
                { "EnumType", "" }
            };
            // Don't call EnsureDefaults() - validation should happen on raw YAML data
            
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsTrue(result.IsValid, "Validation should succeed with only warnings");
            Assert.Greater(result.Warnings.Count, 0, "Should have at least one warning");
            Assert.IsTrue(result.Warnings.Any(w => w.PropertyPath.Contains("EnumColumn") && w.Message.Contains("empty")),
                "Warning should mention empty EnumType");
        }

        [Test]
        public void Validate_ValidSchema_NoErrors()
        {
            // Arrange - valid schema
            var table = new DataTableObj
            {
                Table = "TestTable",
                CodeGenerationOptions = new TableCodeGenerationOptions
                {
                    Namespace = "Test",
                    Class = "TestTable"
                }
            };
            
            table.Columns["Id"] = "Int32";
            table.Columns["Name"] = "String | 256";
            table.Columns["IsActive"] = "bool?";
            table.PrimaryKey.Add("Id");
            table.EnsureDefaults();
            
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsTrue(result.IsValid, "Validation should succeed for valid schema");
            Assert.AreEqual(0, result.Errors.Count, "Should have no errors");
        }

        [Test]
        public void Validate_ComplexTypeWithArrayAndRange_ReportsError()
        {
            // Arrange - column with Complex, Array, and Range modifiers
            var table = new DataTableObj
            {
                Table = "TestTable",
                CodeGenerationOptions = new TableCodeGenerationOptions
                {
                    Namespace = "Test",
                    Class = "TestTable"
                }
            };
            
            table.Columns["BadColumn"] = "Int32[]<> | Complex";
            // Don't call EnsureDefaults() - validation should happen on raw YAML data
            
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for multiple structural modifiers");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
        }

        [Test]
        public void Validate_ErrorIncludesFilePath()
        {
            // Arrange
            var table = new DataTableObj
            {
                Table = "TestTable",
                CodeGenerationOptions = new TableCodeGenerationOptions
                {
                    Namespace = "Test",
                    Class = "TestTable"
                }
            };
            
            table.Columns["BadColumn"] = "String?!";
            // Don't call EnsureDefaults() - validation should happen on raw YAML data
            
            var context = new ValidationContext(table, "path/to/schema.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.Greater(result.Errors.Count, 0);
            Assert.IsTrue(result.Errors.All(e => e.FilePath == "path/to/schema.yaml"),
                "All errors should include the file path");
        }

        [Test]
        public void Validate_ErrorIncludesPropertyPath()
        {
            // Arrange
            var table = new DataTableObj
            {
                Table = "TestTable",
                CodeGenerationOptions = new TableCodeGenerationOptions
                {
                    Namespace = "Test",
                    Class = "TestTable"
                }
            };
            
            table.Columns["MyColumn"] = "String?";
            table.ColumnOptions["MyColumn"] = new System.Collections.Generic.Dictionary<object, object>
            {
                { "AllowNull", false }
            };
            // Don't call EnsureDefaults() - validation should happen on raw YAML data
            
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.Greater(result.Errors.Count, 0);
            Assert.IsTrue(result.Errors.Any(e => e.PropertyPath.Contains("ColumnOptions.MyColumn")),
                "Error should include property path with column name");
        }

        [Test]
        public void Validate_MultipleErrors_AllReported()
        {
            // Arrange - schema with multiple errors
            var table = new DataTableObj
            {
                Table = "TestTable",
                CodeGenerationOptions = new TableCodeGenerationOptions
                {
                    Namespace = "Test",
                    Class = "TestTable"
                }
            };
            
            // Error 1: Conflicting modifiers
            table.Columns["Column1"] = "String?!";
            
            // Error 2: Multiple structural modifiers
            table.Columns["Column2"] = "Int32[]<>";
            
            // Error 3: Conflicting AllowNull
            table.Columns["Column3"] = "String?";
            table.ColumnOptions["Column3"] = new System.Collections.Generic.Dictionary<object, object>
            {
                { "AllowNull", false }
            };
            
            // Don't call EnsureDefaults() - validation should happen on raw YAML data
            
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail");
            Assert.GreaterOrEqual(result.Errors.Count, 3, "Should report all errors, not just the first one");
        }

        #region Helper Classes

        private class MockFileSystemAccessor : IFileSystemAccessor
        {
            public string GetFileContents(string path)
            {
                return string.Empty;
            }
        }

        #endregion
    }
}
