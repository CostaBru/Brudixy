using System.IO;
using System.Linq;
using Brudixy.TypeGenerator.Core;
using Brudixy.TypeGenerator.Core.Validation;
using Brudixy.TypeGenerator.Core.Validation.Rules;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests.Integration
{
    /// <summary>
    /// Integration tests for the complete validation pipeline with real YAML files.
    /// </summary>
    [TestFixture]
    public class ValidationIntegrationTests
    {
        private SchemaValidationEngine _engine;
        private YamlSchemaReader _yamlReader;
        private string _fixturesPath;

        [SetUp]
        public void SetUp()
        {
            _engine = SchemaValidationEngine.CreateDefault();
            _yamlReader = new YamlSchemaReader();
            
            // Get the fixtures directory path
            var testDirectory = TestContext.CurrentContext.TestDirectory;
            _fixturesPath = Path.Combine(testDirectory, "..", "..", "..", "Fixtures");
            _fixturesPath = Path.GetFullPath(_fixturesPath);
        }

        [Test]
        public void Validate_ValidMinimalSchema_NoErrors()
        {
            // Arrange
            var yamlPath = Path.Combine(_fixturesPath, "valid-minimal.st.brudixy.yaml");
            var yamlContent = File.ReadAllText(yamlPath);
            var table = _yamlReader.GetTableForValidation(yamlContent, yamlPath);
            var fileSystem = new TestFileSystemAccessor();
            var context = new ValidationContext(table, yamlPath, fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsTrue(result.IsValid, "Valid schema should pass validation");
            Assert.AreEqual(0, result.Errors.Count, "Should have no errors");
        }

        [Test]
        public void Validate_InvalidColumnReference_ReportsError()
        {
            // Arrange
            var yamlPath = Path.Combine(_fixturesPath, "invalid-column-reference.st.brudixy.yaml");
            var yamlContent = File.ReadAllText(yamlPath);
            var table = _yamlReader.GetTableForValidation(yamlContent, yamlPath);
            var fileSystem = new TestFileSystemAccessor();
            var context = new ValidationContext(table, yamlPath, fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Invalid schema should fail validation");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("NonExistentColumn")),
                "Error should mention the non-existent column");
            Assert.IsTrue(result.Errors.All(e => e.FilePath == yamlPath),
                "All errors should include the correct file path");
        }

        [Test]
        public void Validate_InvalidColumnType_ReportsError()
        {
            // Arrange
            var yamlPath = Path.Combine(_fixturesPath, "invalid-column-type.st.brudixy.yaml");
            var yamlContent = File.ReadAllText(yamlPath);
            var table = _yamlReader.GetTableForValidation(yamlContent, yamlPath);
            var fileSystem = new TestFileSystemAccessor();
            var context = new ValidationContext(table, yamlPath, fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Invalid schema should fail validation");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("nullable") && e.Message.Contains("non-null")),
                "Error should mention conflicting nullable modifiers");
            Assert.IsTrue(result.Errors.All(e => e.FilePath == yamlPath),
                "All errors should include the correct file path");
        }

        [Test]
        public void Validate_ErrorMessageFormat_IncludesAllRequiredInformation()
        {
            // Arrange
            var yamlPath = Path.Combine(_fixturesPath, "invalid-column-reference.st.brudixy.yaml");
            var yamlContent = File.ReadAllText(yamlPath);
            var table = _yamlReader.GetTableForValidation(yamlContent, yamlPath);
            var fileSystem = new TestFileSystemAccessor();
            var context = new ValidationContext(table, yamlPath, fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid);
            var error = result.Errors.First();
            
            // Verify error has all required information
            Assert.IsNotNull(error.FilePath, "Error should have file path");
            Assert.IsNotEmpty(error.FilePath, "File path should not be empty");
            Assert.IsNotNull(error.PropertyPath, "Error should have property path");
            Assert.IsNotEmpty(error.PropertyPath, "Property path should not be empty");
            Assert.IsNotNull(error.Message, "Error should have message");
            Assert.IsNotEmpty(error.Message, "Message should not be empty");
            
            // Verify file path is correct
            Assert.AreEqual(yamlPath, error.FilePath, "File path should match the YAML file");
        }

        [Test]
        public void Validate_MultipleErrors_AllReportedWithCorrectFilePath()
        {
            // Arrange - Create a schema with multiple errors
            var yamlContent = @"
Table: MultiErrorTable
CodeGenerationOptions:
  Namespace: Test.MultiError
  Class: MultiErrorTable
Columns:
  Id: Int32
  BadType: String?!
PrimaryKey:
  - Id
  - NonExistentColumn
Indexes:
  BadIndex:
    Columns:
      - AnotherNonExistentColumn
";
            var yamlPath = "multi-error-test.yaml";
            var table = _yamlReader.GetTableForValidation(yamlContent, yamlPath);
            var fileSystem = new TestFileSystemAccessor();
            var context = new ValidationContext(table, yamlPath, fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Should fail validation");
            Assert.GreaterOrEqual(result.Errors.Count, 3, "Should report all errors");
            Assert.IsTrue(result.Errors.All(e => e.FilePath == yamlPath),
                "All errors should have the correct file path");
        }

        #region Helper Classes

        private class TestFileSystemAccessor : IFileSystemAccessor
        {
            public string GetFileContents(string path)
            {
                if (File.Exists(path))
                {
                    return File.ReadAllText(path);
                }
                return string.Empty;
            }
        }

        #endregion
    }
}
