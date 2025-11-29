using System.Linq;
using Brudixy.TypeGenerator.Core;
using Brudixy.TypeGenerator.Core.Validation;
using Brudixy.TypeGenerator.Core.Validation.Rules;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests.Validation
{
    /// <summary>
    /// Tests for column reference validation in PrimaryKey, Relations, Indexes, and GroupedProperties.
    /// </summary>
    [TestFixture]
    public class ColumnReferenceValidationTests
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
            
            // Register the column reference validation rule
            _engine.RegisterRule(new ColumnReferenceValidationRule());
        }

        [Test]
        public void Validate_PrimaryKeyReferencesNonExistentColumn_ReportsError()
        {
            // Arrange - YAML with PrimaryKey referencing non-existent column
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: TestTable
Columns:
  Id: Int32
  Name: String
PrimaryKey:
  - Id
  - NonExistentColumn
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for non-existent column in PrimaryKey");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("NonExistentColumn") && e.PropertyPath.Contains("PrimaryKey")),
                "Error should mention the non-existent column in PrimaryKey");
        }

        [Test]
        public void Validate_ValidPrimaryKey_NoErrors()
        {
            // Arrange - YAML with valid PrimaryKey
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: TestTable
Columns:
  Id: Int32
  Name: String
PrimaryKey:
  - Id
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsTrue(result.IsValid, "Validation should succeed for valid PrimaryKey");
            Assert.AreEqual(0, result.Errors.Count, "Should have no errors");
        }

        [Test]
        public void Validate_RelationWithNonExistentChildColumn_ReportsError()
        {
            // Arrange - YAML with relation referencing non-existent child column
            var yaml = @"
Table: ChildTable
CodeGenerationOptions:
  Namespace: Test
  Class: ChildTable
Columns:
  Id: Int32
  ParentId: Int32
Relations:
  ParentRelation:
    ParentTable: ParentTable
    ChildTable: ChildTable
    ParentKey:
      - Id
    ChildKey:
      - NonExistentColumn
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for non-existent column in relation");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("NonExistentColumn") && e.PropertyPath.Contains("Relations")),
                "Error should mention the non-existent column in relation");
        }

        [Test]
        public void Validate_RelationWithEmptyParentKey_ReportsError()
        {
            // Arrange - YAML with relation having empty ParentKey
            var yaml = @"
Table: ChildTable
CodeGenerationOptions:
  Namespace: Test
  Class: ChildTable
Columns:
  Id: Int32
  ParentId: Int32
Relations:
  ParentRelation:
    ParentTable: ParentTable
    ChildTable: ChildTable
    ParentKey: []
    ChildKey:
      - ParentId
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for empty ParentKey");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("empty") && e.PropertyPath.Contains("ParentKey")),
                "Error should mention empty ParentKey");
        }

        [Test]
        public void Validate_RelationWithMismatchedKeyLengths_ReportsError()
        {
            // Arrange - YAML with relation having mismatched key lengths
            var yaml = @"
Table: ChildTable
CodeGenerationOptions:
  Namespace: Test
  Class: ChildTable
Columns:
  Id: Int32
  ParentId: Int32
  SecondId: Int32
Relations:
  ParentRelation:
    ParentTable: ParentTable
    ChildTable: ChildTable
    ParentKey:
      - Id
    ChildKey:
      - ParentId
      - SecondId
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for mismatched key lengths");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("mismatched") || e.Message.Contains("length")),
                "Error should mention mismatched key lengths");
        }

        [Test]
        public void Validate_IndexWithNonExistentColumn_ReportsError()
        {
            // Arrange - YAML with index referencing non-existent column
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: TestTable
Columns:
  Id: Int32
  Name: String
Indexes:
  NameIndex:
    Columns:
      - Name
      - NonExistentColumn
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for non-existent column in index");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("NonExistentColumn") && e.PropertyPath.Contains("Indexes")),
                "Error should mention the non-existent column in index");
        }

        [Test]
        public void Validate_GroupedPropertyWithNonExistentColumn_ReportsError()
        {
            // Arrange - YAML with grouped property referencing non-existent column
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: TestTable
Columns:
  FirstName: String
  LastName: String
GroupedProperties:
  FullName: FirstName, LastName, MiddleName
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for non-existent column in grouped property");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("MiddleName") && e.PropertyPath.Contains("GroupedProperties")),
                "Error should mention the non-existent column in grouped property");
        }

        [Test]
        public void Validate_GroupedPropertyWithOneColumn_ReportsWarning()
        {
            // Arrange - YAML with grouped property having only one column
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: TestTable
Columns:
  FirstName: String
  LastName: String
GroupedProperties:
  SingleColumn: FirstName
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsTrue(result.IsValid, "Validation should succeed with only warnings");
            Assert.Greater(result.Warnings.Count, 0, "Should have at least one warning");
            Assert.IsTrue(result.Warnings.Any(w => w.PropertyPath.Contains("SingleColumn") && w.Message.Contains("1 column")),
                "Warning should mention single column in grouped property");
        }

        [Test]
        public void Validate_ValidGroupedProperty_NoErrors()
        {
            // Arrange - YAML with valid grouped property (using pipe separator)
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: TestTable
Columns:
  FirstName: String
  LastName: String
GroupedProperties:
  FullName: FirstName|LastName
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsTrue(result.IsValid, "Validation should succeed for valid grouped property");
            Assert.AreEqual(0, result.Errors.Count, "Should have no errors");
        }

        [Test]
        public void Validate_MultipleColumnReferenceErrors_AllReported()
        {
            // Arrange - YAML with multiple column reference errors
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: TestTable
Columns:
  Id: Int32
PrimaryKey:
  - Id
  - BadColumn1
Indexes:
  BadIndex:
    Columns:
      - BadColumn2
GroupedProperties:
  BadGroup: BadColumn3|BadColumn4
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail");
            Assert.GreaterOrEqual(result.Errors.Count, 4, "Should report all column reference errors");
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
