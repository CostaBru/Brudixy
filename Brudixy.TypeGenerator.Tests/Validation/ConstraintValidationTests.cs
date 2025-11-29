using System.Linq;
using Brudixy.TypeGenerator.Core;
using Brudixy.TypeGenerator.Core.Validation;
using Brudixy.TypeGenerator.Core.Validation.Rules;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests.Validation
{
    /// <summary>
    /// Tests for constraint validation (primary keys, unique columns).
    /// </summary>
    [TestFixture]
    public class ConstraintValidationTests
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
            
            // Register the constraint validation rule
            _engine.RegisterRule(new ConstraintValidationRule());
        }

        [Test]
        public void Validate_NullablePrimaryKeyColumn_ReportsError()
        {
            // Arrange - YAML with nullable primary key column
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: TestTable
Columns:
  Id: Int32?
  Name: String
PrimaryKey:
  - Id
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for nullable primary key");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("Primary key") && e.Message.Contains("nullable")),
                "Error should mention primary key cannot be nullable");
        }

        [Test]
        public void Validate_PrimaryKeyWithAllowNullTrue_ReportsError()
        {
            // Arrange - YAML with primary key column having AllowNull: true
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
ColumnOptions:
  Id:
    AllowNull: true
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for primary key with AllowNull: true");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("Primary key") && e.Message.Contains("AllowNull")),
                "Error should mention primary key cannot have AllowNull: true");
        }

        [Test]
        public void Validate_DuplicateColumnInPrimaryKey_ReportsError()
        {
            // Arrange - YAML with duplicate column in PrimaryKey
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
  - Name
  - Id
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for duplicate column in PrimaryKey");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("duplicate") && e.Message.Contains("Id")),
                "Error should mention duplicate column name");
        }

        [Test]
        public void Validate_NullableUniqueColumn_ReportsError()
        {
            // Arrange - YAML with nullable unique column
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: TestTable
Columns:
  Id: Int32
  Email: String? | Unique
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for nullable unique column");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("Unique") && e.Message.Contains("nullable")),
                "Error should mention unique column cannot be nullable");
        }

        [Test]
        public void Validate_UniqueColumnWithAllowNullTrue_ReportsError()
        {
            // Arrange - YAML with unique column having AllowNull: true
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: TestTable
Columns:
  Id: Int32
  Email: String
ColumnOptions:
  Email:
    IsUnique: true
    AllowNull: true
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for unique column with AllowNull: true");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("Unique") && e.Message.Contains("AllowNull")),
                "Error should mention unique column cannot have AllowNull: true");
        }

        [Test]
        public void Validate_ValidNonNullablePrimaryKey_NoErrors()
        {
            // Arrange - YAML with valid non-nullable primary key
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
            Assert.IsTrue(result.IsValid, "Validation should succeed for valid primary key");
            Assert.AreEqual(0, result.Errors.Count, "Should have no errors");
        }

        [Test]
        public void Validate_ValidNonNullableUniqueColumn_NoErrors()
        {
            // Arrange - YAML with valid non-nullable unique column
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: TestTable
Columns:
  Id: Int32
  Email: String | Unique
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsTrue(result.IsValid, "Validation should succeed for valid unique column");
            Assert.AreEqual(0, result.Errors.Count, "Should have no errors");
        }

        [Test]
        public void Validate_CompositePrimaryKeyWithNullableColumn_ReportsError()
        {
            // Arrange - YAML with composite primary key where one column is nullable
            var yaml = @"
Table: TestTable
CodeGenerationOptions:
  Namespace: Test
  Class: TestTable
Columns:
  Id: Int32
  TenantId: Int32?
  Name: String
PrimaryKey:
  - Id
  - TenantId
";
            
            var table = _yamlReader.GetTableForValidation(yaml, "test.yaml");
            var context = new ValidationContext(table, "test.yaml", _fileSystem);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail for nullable column in composite primary key");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("TenantId") && e.Message.Contains("nullable")),
                "Error should mention the nullable column in primary key");
        }

        [Test]
        public void Validate_PrimaryKeyFromBaseTable_NoErrors()
        {
            // Arrange - Base table with primary key
            var baseYaml = @"
Table: BaseTable
CodeGenerationOptions:
  Namespace: Test
  Class: BaseTable
Columns:
  Id: Int32
  CreatedDate: DateTime
PrimaryKey:
  - Id
";
            
            // Derived table that inherits primary key from base
            var derivedYaml = @"
Table: DerivedTable
CodeGenerationOptions:
  Namespace: Test
  Class: DerivedTable
  BaseTableFileName: base.yaml
Columns:
  Name: String
  Description: String
";
            
            var baseTable = _yamlReader.GetTableForValidation(baseYaml, "base.yaml");
            baseTable.EnsureDefaults();
            
            var derivedTable = _yamlReader.GetTableForValidation(derivedYaml, "derived.yaml");
            
            var loadedBaseTables = new System.Collections.Generic.Dictionary<string, DataTableObj>
            {
                { "base.yaml", baseTable }
            };
            
            var context = new ValidationContext(derivedTable, "derived.yaml", _fileSystem, loadedBaseTables);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsTrue(result.IsValid, "Validation should succeed when primary key is inherited from base table");
            Assert.AreEqual(0, result.Errors.Count, "Should have no errors");
        }

        [Test]
        public void Validate_UniqueColumnInBaseTable_NoErrors()
        {
            // Arrange - Base table with unique column
            var baseYaml = @"
Table: BaseTable
CodeGenerationOptions:
  Namespace: Test
  Class: BaseTable
Columns:
  Id: Int32
  Email: String
ColumnOptions:
  Email:
    IsUnique: true
";
            
            // Derived table
            var derivedYaml = @"
Table: DerivedTable
CodeGenerationOptions:
  Namespace: Test
  Class: DerivedTable
  BaseTableFileName: base.yaml
Columns:
  Name: String
";
            
            var baseTable = _yamlReader.GetTableForValidation(baseYaml, "base.yaml");
            baseTable.EnsureDefaults();
            
            var derivedTable = _yamlReader.GetTableForValidation(derivedYaml, "derived.yaml");
            
            var loadedBaseTables = new System.Collections.Generic.Dictionary<string, DataTableObj>
            {
                { "base.yaml", baseTable }
            };
            
            var context = new ValidationContext(derivedTable, "derived.yaml", _fileSystem, loadedBaseTables);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsTrue(result.IsValid, "Validation should succeed when unique column is in base table");
            Assert.AreEqual(0, result.Errors.Count, "Should have no errors");
        }

        [Test]
        public void Validate_DerivedTableMarksOwnColumnAsUniqueAndNullable_ReportsError()
        {
            // Arrange - Base table
            var baseYaml = @"
Table: BaseTable
CodeGenerationOptions:
  Namespace: Test
  Class: BaseTable
Columns:
  Id: Int32
PrimaryKey:
  - Id
";
            
            // Derived table adds its own unique nullable column (should fail)
            var derivedYaml = @"
Table: DerivedTable
CodeGenerationOptions:
  Namespace: Test
  Class: DerivedTable
  BaseTableFileName: base.yaml
Columns:
  Email: String?
  Name: String
ColumnOptions:
  Email:
    IsUnique: true
";
            
            var baseTable = _yamlReader.GetTableForValidation(baseYaml, "base.yaml");
            baseTable.EnsureDefaults();
            
            var derivedTable = _yamlReader.GetTableForValidation(derivedYaml, "derived.yaml");
            
            var loadedBaseTables = new System.Collections.Generic.Dictionary<string, DataTableObj>
            {
                { "base.yaml", baseTable }
            };
            
            var context = new ValidationContext(derivedTable, "derived.yaml", _fileSystem, loadedBaseTables);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail when derived table defines unique nullable column");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("Email") && e.Message.Contains("nullable")),
                "Error should mention the column cannot be nullable");
        }

        [Test]
        public void Validate_DerivedTableAddsPrimaryKeyWithNullableColumn_ReportsError()
        {
            // Arrange - Base table
            var baseYaml = @"
Table: BaseTable
CodeGenerationOptions:
  Namespace: Test
  Class: BaseTable
Columns:
  Id: Int32
PrimaryKey:
  - Id
";
            
            // Derived table adds its own nullable column to primary key (should fail)
            var derivedYaml = @"
Table: DerivedTable
CodeGenerationOptions:
  Namespace: Test
  Class: DerivedTable
  BaseTableFileName: base.yaml
Columns:
  TenantId: Int32?
  Name: String
PrimaryKey:
  - Id
  - TenantId
";
            
            var baseTable = _yamlReader.GetTableForValidation(baseYaml, "base.yaml");
            baseTable.EnsureDefaults();
            
            var derivedTable = _yamlReader.GetTableForValidation(derivedYaml, "derived.yaml");
            
            var loadedBaseTables = new System.Collections.Generic.Dictionary<string, DataTableObj>
            {
                { "base.yaml", baseTable }
            };
            
            var context = new ValidationContext(derivedTable, "derived.yaml", _fileSystem, loadedBaseTables);

            // Act
            var result = _engine.Validate(context);

            // Assert
            Assert.IsFalse(result.IsValid, "Validation should fail when derived table adds nullable column to primary key");
            Assert.Greater(result.Errors.Count, 0, "Should have at least one error");
            Assert.IsTrue(result.Errors.Any(e => e.Message.Contains("Primary key") && e.Message.Contains("nullable")),
                "Error should mention primary key cannot be nullable");
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
