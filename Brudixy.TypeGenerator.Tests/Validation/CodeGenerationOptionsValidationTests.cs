using System.Linq;
using Brudixy.TypeGenerator.Core;
using Brudixy.TypeGenerator.Core.Validation;
using Brudixy.TypeGenerator.Core.Validation.Rules;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests.Validation
{
    /// <summary>
    /// Tests for code generation options validation.
    /// </summary>
    [TestFixture]
    public class CodeGenerationOptionsValidationTests
    {
        private SchemaValidationEngine _engine;
        private MockFileSystemAccessor _fileSystem;
        private YamlSchemaReader _yamlReader;

        [SetUp]
        public void SetUp()
        {
            _engine = new SchemaValidationEngine();
            _fileSystem = new MockFileSystemAccessor();
            _yamlReader = new YamlSchemaReader();
            
            // Register the code generation options validation rule
            _engine.RegisterRule(new CodeGenerationOptionsValidationRule());
        }

        [Test]
        public void Validate_InvalidNamespace_ReportsError()
        {
            // Arrange - YAML with invalid namespace (starts with digit)
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: 123Invalid
  Class: TestTable
Columns:
  Id: Int32
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for invalid namespace");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("Namespace") && e.Message.Contains("naming conventions")),
                "Error should mention namespace naming conventions");
        }

        [Test]
        public void Validate_InvalidClassName_ReportsError()
        {
            // Arrange - YAML with invalid class name (contains special characters)
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: Test-Table
Columns:
  Id: Int32
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for invalid class name");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("Class") && e.Message.Contains("identifier")),
                "Error should mention class identifier rules");
        }

        [Test]
        public void Validate_ClassNameIsKeyword_ReportsError()
        {
            // Arrange - YAML with class name that is a C# keyword
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: class
Columns:
  Id: Int32
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for keyword as class name");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("keyword")),
                "Error should mention C# keyword");
        }

        [Test]
        public void Validate_AbstractAndSealed_ReportsError()
        {
            // Arrange - YAML with both Abstract and Sealed set to true
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: TestTable
  Abstract: true
  Sealed: true
Columns:
  Id: Int32
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for Abstract and Sealed both true");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("Abstract") && e.Message.Contains("Sealed")),
                "Error should mention Abstract and Sealed conflict");
        }

        [Test]
        public void Validate_ValidNamespaceAndClass_NoErrors()
        {
            // Arrange - YAML with valid namespace and class name
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: MyCompany.MyProject.Data
  Class: TestTable
  RowClass: TestTableRow
Columns:
  Id: Int32
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsTrue(result.IsValid, "Validation should succeed for valid options");
            Assert.AreEqual(0, result.Errors.Count, "Should have no errors");
        }

        [Test]
        public void Validate_NamespaceWithKeyword_ReportsWarning()
        {
            // Arrange - YAML with namespace containing a C# keyword
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: MyCompany.class.Data
  Class: TestTable
  RowClass: TestTableRow
Columns:
  Id: Int32
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            // Note: IsValid might be false if other required fields are missing, so just check for warnings
            Assert.Greater(result.Warnings.Count, 0, "Should have at least one warning");
            Assert.IsTrue(result.Warnings.Any(w => w.Message.Contains("keyword")),
                "Warning should mention C# keyword");
        }

        [Test]
        public void Validate_InvalidExtraUsing_ReportsWarning()
        {
            // Arrange - YAML with invalid ExtraUsing entry
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: TestTable
  RowClass: TestTableRow
  ExtraUsing:
    - System.Collections.Generic
    - 123Invalid
Columns:
  Id: Int32
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            // Note: IsValid might be false if other required fields are missing, so just check for warnings
            Assert.Greater(result.Warnings.Count, 0, "Should have at least one warning");
            Assert.IsTrue(result.Warnings.Any(w => w.Message.Contains("ExtraUsing") && w.Message.Contains("123Invalid")),
                "Warning should mention invalid ExtraUsing entry");
        }

        [Test]
        public void Validate_MissingNamespace_NoErrors()
        {
            // Arrange - YAML without namespace (namespace is optional and can be derived from file path)
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Class: TestTable
  RowClass: TestTableRow
Columns:
  Id: Int32
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsTrue(result.IsValid, "Validation should succeed when namespace is missing (it will be derived from file path)");
            Assert.AreEqual(0, result.Errors.Count, "Should have no errors");
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
