using System;
using System.IO;
using System.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using Brudixy.Serialization;
using NUnit.Framework;

namespace Brudixy.Tests.YamlSchemaLoading;

/// <summary>
/// Unit tests for YamlSchemaLoader
/// Requirements: 8.1, 8.2, 8.3, 8.4, 8.5
/// </summary>
[TestFixture]
public class YamlSchemaLoaderTests
{
    private YamlSchemaLoader _loader;
    
    [SetUp]
    public void SetUp()
    {
        _loader = new YamlSchemaLoader();
    }
    
    #region Simple Table Loading Tests
    
    [Test]
    public void LoadIntoTable_WithSimpleSchema_CreatesTableStructure()
    {
        // Arrange
        var table = new DataTable("TestTable");
        var yaml = @"
TableName: SimpleTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
  - Name: Name
    Type: String
    AllowNull: false
";
        
        // Act
        _loader.LoadIntoTable(table, yaml);
        
        // Assert
        Assert.That(table.Name, Is.EqualTo("SimpleTable"));
        Assert.That(table.GetColumns().Count(), Is.EqualTo(2));
        Assert.That(table.TryGetColumn("Id"), Is.Not.Null);
        Assert.That(table.TryGetColumn("Name"), Is.Not.Null);
    }
    
    [Test]
    public void LoadIntoTable_WithNullTable_ThrowsArgumentNullException()
    {
        // Arrange
        var yaml = "TableName: Test\nColumns:\n  - Name: Id\n    Type: Int32\n    AllowNull: false";
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _loader.LoadIntoTable(null, yaml));
    }
    
    [Test]
    public void LoadIntoTable_WithEmptyYaml_ThrowsArgumentException()
    {
        // Arrange
        var table = new DataTable("Test");
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _loader.LoadIntoTable(table, ""));
    }
    
    [Test]
    public void LoadIntoTable_WithInvalidYaml_ThrowsSchemaValidationException()
    {
        // Arrange
        var table = new DataTable("Test");
        var yaml = @"
TableName: Test
Columns:
  - Name: Id
    Type: InvalidType
    AllowNull: false
";
        
        // Act & Assert
        Assert.Throws<SchemaValidationException>(() => _loader.LoadIntoTable(table, yaml));
    }
    
    [Test]
    public void LoadIntoTable_WithMalformedYaml_ThrowsYamlParsingException()
    {
        // Arrange
        var table = new DataTable("Test");
        var yaml = @"
TableName: Test
Columns:
  - Name: Id
    Type: Int32
    AllowNull: [invalid
";
        
        // Act & Assert
        Assert.Throws<YamlParsingException>(() => _loader.LoadIntoTable(table, yaml));
    }
    
    #endregion
    
    #region Table with All Features Tests
    
    [Test]
    public void LoadIntoTable_WithAllFeatures_CreatesCompleteTable()
    {
        // Arrange
        var table = new DataTable("CompleteTable");
        var yaml = @"
TableName: CompleteTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
  - Name: Name
    Type: String
    AllowNull: false
  - Name: Email
    Type: String
    AllowNull: false
  - Name: Age
    Type: Int32
    AllowNull: true
  - Name: IsActive
    Type: Boolean
    AllowNull: false
PrimaryKey:
  - Id
Indexes:
  - Name: IX_Email
    Columns:
      - Email
    IsUnique: true
ColumnOptions:
  Name:
    MaxLength: 100
  Email:
    IsUnique: true
  IsActive:
    DefaultValue: true
XProperties:
  - Name: Description
    Type: String
    Value: Test table
  - Name: Version
    Type: Int32
    Value: 1
";
        
        // Act
        _loader.LoadIntoTable(table, yaml);
        
        // Assert
        Assert.That(table.Name, Is.EqualTo("CompleteTable"));
        Assert.That(table.GetColumns().Count(), Is.EqualTo(5));
        
        // Check columns exist
        Assert.That(table.TryGetColumn("Id"), Is.Not.Null);
        Assert.That(table.TryGetColumn("Name"), Is.Not.Null);
        Assert.That(table.TryGetColumn("Email"), Is.Not.Null);
        Assert.That(table.TryGetColumn("Age"), Is.Not.Null);
        Assert.That(table.TryGetColumn("IsActive"), Is.Not.Null);
        
        // Check primary key
        var pkColumns = table.PrimaryKey.ToArray();
        Assert.That(pkColumns, Has.Length.EqualTo(1));
        Assert.That(pkColumns[0].ColumnName, Is.EqualTo("Id"));
        
        // Check indexes
        Assert.That(table.HasIndex("Email"), Is.True);
        
        // Check extended properties
        Assert.That(table.GetXProperty<string>("Description"), Is.EqualTo("Test table"));
        Assert.That(table.GetXProperty<int>("Version"), Is.EqualTo(1));
    }
    
    [Test]
    public void LoadIntoTable_WithPrimaryKey_ConfiguresPrimaryKey()
    {
        // Arrange
        var table = new DataTable("TestTable");
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
  - Name: Name
    Type: String
    AllowNull: false
PrimaryKey:
  - Id
";
        
        // Act
        _loader.LoadIntoTable(table, yaml);
        
        // Assert
        var pkColumns = table.PrimaryKey.ToArray();
        Assert.That(pkColumns, Has.Length.EqualTo(1));
        Assert.That(pkColumns[0].ColumnName, Is.EqualTo("Id"));
    }
    
    [Test]
    public void LoadIntoTable_WithIndexes_CreatesIndexes()
    {
        // Arrange
        var table = new DataTable("TestTable");
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
  - Name: Email
    Type: String
    AllowNull: false
  - Name: Name
    Type: String
    AllowNull: false
Indexes:
  - Name: IX_Email
    Columns:
      - Email
    IsUnique: true
  - Name: IX_Name
    Columns:
      - Name
    IsUnique: false
";
        
        // Act
        _loader.LoadIntoTable(table, yaml);
        
        // Assert
        Assert.That(table.HasIndex("Email"), Is.True);
        Assert.That(table.HasIndex("Name"), Is.True);
    }
    
    [Test]
    public void LoadIntoTable_WithXProperties_AttachesExtendedProperties()
    {
        // Arrange
        var table = new DataTable("TestTable");
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
XProperties:
  - Name: Description
    Type: String
    Value: Test description
  - Name: Version
    Type: Int32
    Value: 42
  - Name: IsEnabled
    Type: Boolean
    Value: true
";
        
        // Act
        _loader.LoadIntoTable(table, yaml);
        
        // Assert
        Assert.That(table.GetXProperty<string>("Description"), Is.EqualTo("Test description"));
        Assert.That(table.GetXProperty<int>("Version"), Is.EqualTo(42));
        Assert.That(table.GetXProperty<bool>("IsEnabled"), Is.EqualTo(true));
    }
    
    [Test]
    public void LoadIntoTable_WithColumnOptions_AppliesConstraints()
    {
        // Arrange
        var table = new DataTable("TestTable");
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
  - Name: Name
    Type: String
    AllowNull: false
  - Name: Email
    Type: String
    AllowNull: false
ColumnOptions:
  Name:
    MaxLength: 100
    HasIndex: true
  Email:
    IsUnique: true
    HasIndex: true
";
        
        // Act
        _loader.LoadIntoTable(table, yaml);
        
        // Assert
        var nameColumn = table.GetColumn("Name");
        Assert.That(nameColumn.MaxLength, Is.EqualTo(100));
        Assert.That(table.HasIndex("Name"), Is.True);
        Assert.That(table.HasIndex("Email"), Is.True);
    }
    
    #endregion
    
    #region File Loading Tests
    
    [Test]
    public void LoadIntoTableFromFile_WithValidFile_LoadsSchema()
    {
        // Arrange
        var table = new DataTable("TestTable");
        var filePath = Path.Combine("YamlSchemaLoading", "Fixtures", "SimpleTable.yaml");
        
        // Act
        _loader.LoadIntoTableFromFile(table, filePath);
        
        // Assert
        Assert.That(table.Name, Is.EqualTo("SimpleTable"));
        Assert.That(table.GetColumns().Count(), Is.GreaterThan(0));
    }
    
    [Test]
    public void LoadIntoTableFromFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var table = new DataTable("Test");
        var filePath = "NonExistent.yaml";
        
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _loader.LoadIntoTableFromFile(table, filePath));
    }
    
    #endregion
    
    #region Child Table Loading Tests
    
    [Test]
    public void LoadAsChildTable_WithValidYaml_CreatesChildTable()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        var yaml = @"
TableName: ChildTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
  - Name: Name
    Type: String
    AllowNull: false
";
        
        // Act
        var childTable = _loader.LoadAsChildTable(dataset, yaml);
        
        // Assert
        Assert.That(childTable, Is.Not.Null);
        Assert.That(childTable.Name, Is.EqualTo("ChildTable"));
        Assert.That(dataset.HasTable("ChildTable"), Is.True);
    }
    
    [Test]
    public void LoadAsChildTable_WithNullDataset_ThrowsArgumentNullException()
    {
        // Arrange
        var yaml = "TableName: Test\nColumns:\n  - Name: Id\n    Type: Int32\n    AllowNull: false";
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _loader.LoadAsChildTable(null, yaml));
    }
    
    [Test]
    public void LoadAsChildTableFromFile_WithValidFile_CreatesChildTable()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        var filePath = Path.Combine("YamlSchemaLoading", "Fixtures", "SimpleTable.yaml");
        
        // Act
        var childTable = _loader.LoadAsChildTableFromFile(dataset, filePath);
        
        // Assert
        Assert.That(childTable, Is.Not.Null);
        Assert.That(dataset.HasTable(childTable.Name), Is.True);
    }
    
    #endregion
    
    #region Multiple Tables with Relations Tests
    
    [Test]
    public void LoadMultipleTables_WithValidSchemas_CreatesAllTables()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        var parentYaml = @"
TableName: ParentTable
Columns:
  - Name: ParentId
    Type: Int32
    AllowNull: false
  - Name: ParentName
    Type: String
    AllowNull: false
PrimaryKey:
  - ParentId
";
        
        var childYaml = @"
TableName: ChildTable
Columns:
  - Name: ChildId
    Type: Int32
    AllowNull: false
  - Name: ParentId
    Type: Int32
    AllowNull: false
  - Name: ChildName
    Type: String
    AllowNull: false
PrimaryKey:
  - ChildId
";
        
        // Act
        _loader.LoadMultipleTables(dataset, new[] { parentYaml, childYaml });
        
        // Assert
        Assert.That(dataset.HasTable("ParentTable"), Is.True);
        Assert.That(dataset.HasTable("ChildTable"), Is.True);
    }
    
    [Test]
    public void LoadMultipleTables_WithRelations_EstablishesRelations()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        var parentYaml = @"
TableName: ParentTable
Columns:
  - Name: ParentId
    Type: Int32
    AllowNull: false
  - Name: ParentName
    Type: String
    AllowNull: false
PrimaryKey:
  - ParentId
";
        
        var childYaml = @"
TableName: ChildTable
Columns:
  - Name: ChildId
    Type: Int32
    AllowNull: false
  - Name: ParentId
    Type: Int32
    AllowNull: false
  - Name: ChildName
    Type: String
    AllowNull: false
PrimaryKey:
  - ChildId
Relations:
  - Name: FK_Child_Parent
    ParentTable: ParentTable
    ParentColumns:
      - ParentId
    ChildColumns:
      - ParentId
";
        
        // Act
        _loader.LoadMultipleTables(dataset, new[] { parentYaml, childYaml });
        
        // Assert
        var childTable = dataset.GetTable("ChildTable");
        var relations = childTable.ParentRelations.ToArray();
        Assert.That(relations, Is.Not.Empty);
    }
    
    [Test]
    public void LoadMultipleTables_WithInvalidRelation_ThrowsRelationException()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        var parentYaml = @"
TableName: ParentTable
Columns:
  - Name: ParentId
    Type: Int32
    AllowNull: false
PrimaryKey:
  - ParentId
";
        
        var childYaml = @"
TableName: ChildTable
Columns:
  - Name: ChildId
    Type: Int32
    AllowNull: false
  - Name: ParentId
    Type: Int32
    AllowNull: false
PrimaryKey:
  - ChildId
Relations:
  - Name: FK_Child_NonExistent
    ParentTable: NonExistentTable
    ParentColumns:
      - ParentId
    ChildColumns:
      - ParentId
";
        
        // Act & Assert
        Assert.Throws<RelationException>(() => 
            _loader.LoadMultipleTables(dataset, new[] { parentYaml, childYaml }));
    }
    
    [Test]
    public void LoadMultipleTables_WithNullDataset_ThrowsArgumentNullException()
    {
        // Arrange
        var yaml = "TableName: Test\nColumns:\n  - Name: Id\n    Type: Int32\n    AllowNull: false";
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _loader.LoadMultipleTables(null, new[] { yaml }));
    }
    
    [Test]
    public void LoadMultipleTables_WithNullYamlContents_ThrowsArgumentNullException()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _loader.LoadMultipleTables(dataset, null));
    }
    
    #endregion
    
    #region Error Handling Tests
    
    [Test]
    public void LoadIntoTable_WithInvalidColumnType_ThrowsColumnTypeException()
    {
        // Arrange
        var table = new DataTable("TestTable");
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
  - Name: BadColumn
    Type: InvalidType123
    AllowNull: false
";
        
        // Act & Assert
        var ex = Assert.Throws<SchemaValidationException>(() => _loader.LoadIntoTable(table, yaml));
        Assert.That(ex.Errors, Is.Not.Empty);
    }
    
    [Test]
    public void LoadMultipleTables_WithMissingParentTable_ThrowsRelationException()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        var childYaml = @"
TableName: ChildTable
Columns:
  - Name: ChildId
    Type: Int32
    AllowNull: false
  - Name: ParentId
    Type: Int32
    AllowNull: false
Relations:
  - Name: FK_Child_Parent
    ParentTable: ParentTable
    ParentColumns:
      - ParentId
    ChildColumns:
      - ParentId
";
        
        // Act & Assert
        Assert.Throws<RelationException>(() => 
            _loader.LoadMultipleTables(dataset, new[] { childYaml }));
    }
    
    [Test]
    public void LoadMultipleTables_WithMissingColumn_ThrowsRelationException()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        var parentYaml = @"
TableName: ParentTable
Columns:
  - Name: ParentId
    Type: Int32
    AllowNull: false
PrimaryKey:
  - ParentId
";
        
        var childYaml = @"
TableName: ChildTable
Columns:
  - Name: ChildId
    Type: Int32
    AllowNull: false
  - Name: ParentId
    Type: Int32
    AllowNull: false
Relations:
  - Name: FK_Child_Parent
    ParentTable: ParentTable
    ParentColumns:
      - NonExistentColumn
    ChildColumns:
      - ParentId
";
        
        // Act & Assert
        Assert.Throws<RelationException>(() => 
            _loader.LoadMultipleTables(dataset, new[] { parentYaml, childYaml }));
    }
    
    #endregion
    
    #region Column Type Tests
    
    [Test]
    public void LoadIntoTable_WithVariousColumnTypes_CreatesCorrectColumns()
    {
        // Arrange
        var table = new DataTable("TestTable");
        var yaml = @"
TableName: TestTable
Columns:
  - Name: IntCol
    Type: Int32
    AllowNull: false
  - Name: LongCol
    Type: Int64
    AllowNull: false
  - Name: StringCol
    Type: String
    AllowNull: false
  - Name: DateCol
    Type: DateTime
    AllowNull: false
  - Name: BoolCol
    Type: Boolean
    AllowNull: false
  - Name: GuidCol
    Type: Guid
    AllowNull: false
  - Name: DecimalCol
    Type: Decimal
    AllowNull: false
  - Name: DoubleCol
    Type: Double
    AllowNull: false
";
        
        // Act
        _loader.LoadIntoTable(table, yaml);
        
        // Assert
        Assert.That(table.TryGetColumn("IntCol"), Is.Not.Null);
        Assert.That(table.TryGetColumn("LongCol"), Is.Not.Null);
        Assert.That(table.TryGetColumn("StringCol"), Is.Not.Null);
        Assert.That(table.TryGetColumn("DateCol"), Is.Not.Null);
        Assert.That(table.TryGetColumn("BoolCol"), Is.Not.Null);
        Assert.That(table.TryGetColumn("GuidCol"), Is.Not.Null);
        Assert.That(table.TryGetColumn("DecimalCol"), Is.Not.Null);
        Assert.That(table.TryGetColumn("DoubleCol"), Is.Not.Null);
    }
    
    #endregion
    
    #region CodeGenerationOptions Tests
    
    [Test]
    public void LoadIntoTable_WithCodeGenerationOptions_StoresAsExtendedProperties()
    {
        // Arrange
        var table = new DataTable("TestTable");
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
CodeGenerationOptions:
  BaseNamespace: MyApp.Data
  Namespace: MyApp.Data.Tables
  Class: TestTableClass
  RowClass: TestTableRow
  Abstract: false
  Sealed: true
";
        
        // Act
        _loader.LoadIntoTable(table, yaml);
        
        // Assert
        Assert.That(table.Namespace, Is.EqualTo("MyApp.Data"));
      }
    
    #endregion
}
