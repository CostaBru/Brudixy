using System.Collections.Generic;
using System.Linq;
using Brudixy.TypeGenerator.Core;
using Brudixy.TypeGenerator.Core.Validation;
using Brudixy.TypeGenerator.Core.Validation.Rules;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests.Validation
{
    /// <summary>
    /// Tests for dataset validation (unique table names, relation integrity).
    /// </summary>
    [TestFixture]
    public class DatasetValidationTests
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
            
            // Register the dataset validation rule
            _engine.RegisterRule(new DatasetValidationRule());
        }

        [Test]
        public void Validate_DuplicateTableNames_ReportsError()
        {
            // Arrange - Dataset with duplicate table names
            var yaml = @"
Table: TestDataset
CodeGenerationOptions:
  Namespace: Test
  Class: TestDataset
Tables:
  - t_user
  - t_order
  - t_user
";
            
            var dataset = _yamlReader.GetTableForValidation(yaml, "test.ds.yaml");
            var context = new ValidationContext(dataset, "test.ds.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for duplicate table names");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("duplicate") && e.Message.Contains("t_user")),
                "Error should mention duplicate table name");
        }

        [Test]
        public void Validate_UniqueTableNames_NoErrors()
        {
            // Arrange - Dataset with unique table names
            var yaml = @"
Table: TestDataset
CodeGenerationOptions:
  Namespace: Test
  Class: TestDataset
Tables:
  - t_user
  - t_order
  - t_product
";
            
            var dataset = _yamlReader.GetTableForValidation(yaml, "test.ds.yaml");
            var context = new ValidationContext(dataset, "test.ds.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsTrue(result.IsValid, "Validation should succeed for unique table names");
            Assert.AreEqual(0, result.Errors.Count, "Should have no errors");
        }

        [Test]
        public void Validate_RelationReferencesNonExistentParentTable_ReportsError()
        {
            // Arrange - Dataset with relation referencing non-existent parent table
            var yaml = @"
Table: TestDataset
CodeGenerationOptions:
  Namespace: Test
  Class: TestDataset
Tables:
  - t_user
  - t_order
Relations:
  FK_Order_Customer:
    ParentTable: t_customer
    ChildTable: t_order
    ParentKey:
      - id
    ChildKey:
      - customer_id
";
            
            var dataset = _yamlReader.GetTableForValidation(yaml, "test.ds.yaml");
            var context = new ValidationContext(dataset, "test.ds.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for non-existent parent table");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("t_customer") && e.Message.Contains("not in the dataset")),
                "Error should mention table not in dataset");
        }

        [Test]
        public void Validate_RelationReferencesNonExistentChildTable_ReportsError()
        {
            // Arrange - Dataset with relation referencing non-existent child table
            var yaml = @"
Table: TestDataset
CodeGenerationOptions:
  Namespace: Test
  Class: TestDataset
Tables:
  - t_user
  - t_order
Relations:
  FK_OrderItem_Order:
    ParentTable: t_order
    ChildTable: t_order_item
    ParentKey:
      - id
    ChildKey:
      - order_id
";
            
            var dataset = _yamlReader.GetTableForValidation(yaml, "test.ds.yaml");
            var context = new ValidationContext(dataset, "test.ds.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for non-existent child table");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("t_order_item") && e.Message.Contains("not in the dataset")),
                "Error should mention table not in dataset");
        }

        [Test]
        public void Validate_RelationWithValidTables_NoErrors()
        {
            // Arrange - Dataset with valid relation
            var yaml = @"
Table: TestDataset
CodeGenerationOptions:
  Namespace: Test
  Class: TestDataset
Tables:
  - t_user
  - t_order
Relations:
  FK_Order_User:
    ParentTable: t_user
    ChildTable: t_order
    ParentKey:
      - id
    ChildKey:
      - user_id
";
            
            var dataset = _yamlReader.GetTableForValidation(yaml, "test.ds.yaml");
            var context = new ValidationContext(dataset, "test.ds.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsTrue(result.IsValid, "Validation should succeed for valid relation");
            Assert.AreEqual(0, result.Errors.Count, "Should have no errors");
        }

        [Test]
        public void Validate_RelationWithNonExistentColumn_ReportsError()
        {
            // Arrange - Setup table schemas
            var userTableYaml = @"
Table: t_user
CodeGenerationOptions:
  Namespace: Test
  Class: UserTable
Columns:
  id: Int32
  name: String
PrimaryKey:
  - id
";

            var orderTableYaml = @"
Table: t_order
CodeGenerationOptions:
  Namespace: Test
  Class: OrderTable
Columns:
  id: Int32
  user_id: Int32
  total: Decimal
PrimaryKey:
  - id
";

            _fileSystem.AddFile("t_user.dt.brudixy.yaml", userTableYaml);
            _fileSystem.AddFile("t_order.dt.brudixy.yaml", orderTableYaml);

            var datasetYaml = @"
Table: TestDataset
CodeGenerationOptions:
  Namespace: Test
  Class: TestDataset
Tables:
  - t_user
  - t_order
Relations:
  FK_Order_User:
    ParentTable: t_user
    ChildTable: t_order
    ParentKey:
      - customer_id
    ChildKey:
      - user_id
";
            
            var dataset = _yamlReader.GetTableForValidation(datasetYaml, "test.ds.yaml");
            var context = new ValidationContext(dataset, "test.ds.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for non-existent column");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("customer_id") && e.Message.Contains("does not exist")),
                "Error should mention non-existent column");
        }

        [Test]
        public void Validate_RelationWithIncompatibleTypes_ReportsError()
        {
            // Arrange - Setup table schemas with incompatible types
            var userTableYaml = @"
Table: t_user
CodeGenerationOptions:
  Namespace: Test
  Class: UserTable
Columns:
  id: String
  name: String
PrimaryKey:
  - id
";

            var orderTableYaml = @"
Table: t_order
CodeGenerationOptions:
  Namespace: Test
  Class: OrderTable
Columns:
  id: Int32
  user_id: Int32
  total: Decimal
PrimaryKey:
  - id
";

            _fileSystem.AddFile("t_user.dt.brudixy.yaml", userTableYaml);
            _fileSystem.AddFile("t_order.dt.brudixy.yaml", orderTableYaml);

            var datasetYaml = @"
Table: TestDataset
CodeGenerationOptions:
  Namespace: Test
  Class: TestDataset
Tables:
  - t_user
  - t_order
Relations:
  FK_Order_User:
    ParentTable: t_user
    ChildTable: t_order
    ParentKey:
      - id
    ChildKey:
      - user_id
";
            
            var dataset = _yamlReader.GetTableForValidation(datasetYaml, "test.ds.yaml");
            var context = new ValidationContext(dataset, "test.ds.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for incompatible types");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("incompatible") && e.Message.Contains("type")),
                "Error should mention incompatible types");
        }

        [Test]
        public void Validate_RelationWithCompatibleTypes_NoErrors()
        {
            // Arrange - Setup table schemas with compatible types
            var userTableYaml = @"
Table: t_user
CodeGenerationOptions:
  Namespace: Test
  Class: UserTable
Columns:
  id: Int32
  name: String
PrimaryKey:
  - id
";

            var orderTableYaml = @"
Table: t_order
CodeGenerationOptions:
  Namespace: Test
  Class: OrderTable
Columns:
  id: Int32
  user_id: Int32
  total: Decimal
PrimaryKey:
  - id
";

            _fileSystem.AddFile("t_user.dt.brudixy.yaml", userTableYaml);
            _fileSystem.AddFile("t_order.dt.brudixy.yaml", orderTableYaml);

            var datasetYaml = @"
Table: TestDataset
CodeGenerationOptions:
  Namespace: Test
  Class: TestDataset
Tables:
  - t_user
  - t_order
Relations:
  FK_Order_User:
    ParentTable: t_user
    ChildTable: t_order
    ParentKey:
      - id
    ChildKey:
      - user_id
";
            
            var dataset = _yamlReader.GetTableForValidation(datasetYaml, "test.ds.yaml");
            var context = new ValidationContext(dataset, "test.ds.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsTrue(result.IsValid, "Validation should succeed for compatible types");
            Assert.AreEqual(0, result.Errors.Count, "Should have no errors");
        }

        #region Helper Classes

        private class MockFileSystemAccessor : IFileSystemAccessor
        {
            private readonly Dictionary<string, string> _files = new Dictionary<string, string>();

            public void AddFile(string path, string content)
            {
                _files[path] = content;
            }

            public string GetFileContents(string path)
            {
                var fileName = System.IO.Path.GetFileName(path);
                if (_files.ContainsKey(fileName))
                {
                    return _files[fileName];
                }
                return string.Empty;
            }
        }

        #endregion
    }
}
