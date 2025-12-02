using System;
using System.IO;
using Brudixy;
using Brudixy.Serialization;
using NUnit.Framework;

namespace Brudixy.Tests.YamlSchemaLoading;

/// <summary>
/// Unit tests for error handling in YAML schema loading
/// Tests SchemaNotFoundException, YamlParsingException, RelationException, and ColumnTypeException
/// </summary>
[TestFixture]
public class ErrorHandlingTests
{
    [Test]
    public void SchemaValidator_ThrowsSchemaNotFoundException_WhenSchemaFileMissing()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        
        // Act & Assert
        var exception = Assert.Throws<SchemaNotFoundException>(() => new SchemaValidator(nonExistentPath));
        Assert.AreEqual(nonExistentPath, exception.SchemaFilePath);
        Assert.That(exception.Message, Does.Contain("not found").IgnoreCase);
    }
    
    [Test]
    public void YamlSchemaLoader_ThrowsYamlParsingException_ForMalformedYaml()
    {
        // Arrange
        var loader = new YamlSchemaLoader();
        var table = new DataTable("TestTable");
        
        // Malformed YAML with invalid indentation
        var malformedYaml = @"
Table: TestTable
Columns:
  Id:
    Type: Int32
  Name:
      Type: String
    InvalidIndent: true
";
        
        // Act & Assert
        var exception = Assert.Throws<YamlParsingException>(() => loader.LoadIntoTable(table, malformedYaml));
        Assert.IsNotNull(exception.YamlContent);
        Assert.AreEqual(malformedYaml, exception.YamlContent);
    }
    
    [Test]
    public void YamlSchemaLoader_ThrowsYamlParsingException_ForInvalidYamlSyntax()
    {
        // Arrange
        var loader = new YamlSchemaLoader();
        var table = new DataTable("TestTable");
        
        // Invalid YAML with unclosed quotes
        var invalidYaml = @"
Table: ""TestTable
Columns:
  Id:
    Type: Int32
";
        
        // Act & Assert
        var exception = Assert.Throws<YamlParsingException>(() => loader.LoadIntoTable(table, invalidYaml));
        Assert.IsNotNull(exception.YamlContent);
    }
    
    [Test]
    public void YamlSchemaLoader_ThrowsRelationException_ForInvalidColumnReferences()
    {
        // Arrange
        var loader = new YamlSchemaLoader();
        var dataset = new DataTable("Dataset");
        
        // Create parent table
        var parentYaml = @"
Table: ParentTable
Columns:
  Id:
    Type: Int32
  Name:
    Type: String
PrimaryKey:
  - Id
";
        
        // Create child table with relation referencing non-existent column
        var childYaml = @"
Table: ChildTable
Columns:
  Id:
    Type: Int32
  ParentId:
    Type: Int32
Relations:
  ParentRelation:
    ParentTable: ParentTable
    ChildTable: ChildTable
    ParentKey:
      - NonExistentColumn
    ChildKey:
      - ParentId
";
        
        // Act & Assert
        var exception = Assert.Throws<RelationException>(() => 
            loader.LoadMultipleTables(dataset, new[] { parentYaml, childYaml }));
        
        Assert.AreEqual("ParentRelation", exception.RelationName);
        Assert.IsNotEmpty(exception.Errors);
        Assert.IsNotEmpty(exception.AvailableTables);
        Assert.That(exception.Message, Does.Contain("NonExistentColumn").IgnoreCase);
    }
    
    [Test]
    public void YamlSchemaLoader_ThrowsRelationException_ForNonExistentParentTable()
    {
        // Arrange
        var loader = new YamlSchemaLoader();
        var dataset = new DataTable("Dataset");
        
        // Create child table with relation referencing non-existent parent table
        var childYaml = @"
Table: ChildTable
Columns:
  Id:
    Type: Int32
  ParentId:
    Type: Int32
Relations:
  ParentRelation:
    ParentTable: NonExistentTable
    ChildTable: ChildTable
    ParentKey:
      - Id
    ChildKey:
      - ParentId
";
        
        // Act & Assert
        var exception = Assert.Throws<RelationException>(() => 
            loader.LoadMultipleTables(dataset, new[] { childYaml }));
        
        Assert.AreEqual("ParentRelation", exception.RelationName);
        Assert.That(exception.Message, Does.Contain("NonExistentTable").IgnoreCase);
    }
    
    [Test]
    public void YamlSchemaLoader_ThrowsRelationException_ForNonExistentChildTable()
    {
        // Arrange
        var loader = new YamlSchemaLoader();
        var dataset = new DataTable("Dataset");
        
        // Create parent table
        var parentYaml = @"
Table: ParentTable
Columns:
  Id:
    Type: Int32
Relations:
  ChildRelation:
    ParentTable: ParentTable
    ChildTable: NonExistentChildTable
    ParentKey:
      - Id
    ChildKey:
      - ParentId
";
        
        // Act & Assert
        var exception = Assert.Throws<RelationException>(() => 
            loader.LoadMultipleTables(dataset, new[] { parentYaml }));
        
        Assert.AreEqual("ChildRelation", exception.RelationName);
        Assert.That(exception.Message, Does.Contain("NonExistentChildTable").IgnoreCase);
    }
    
    [Test]
    public void YamlSchemaLoader_ThrowsColumnTypeException_ForInvalidType()
    {
        // Arrange
        var loader = new YamlSchemaLoader();
        var table = new DataTable("TestTable");
        
        // YAML with invalid column type
        var yamlWithInvalidType = @"
Table: TestTable
Columns:
  Id:
    Type: InvalidType
";
        
        // Act & Assert
        var exception = Assert.Throws<ColumnTypeException>(() => loader.LoadIntoTable(table, yamlWithInvalidType));
        Assert.AreEqual("Id", exception.ColumnName);
        Assert.AreEqual("InvalidType", exception.InvalidType);
        Assert.That(exception.Message, Does.Contain("invalid type").IgnoreCase);
    }
    
    [Test]
    public void YamlSchemaLoader_ThrowsColumnTypeException_ForInvalidTypeModifier()
    {
        // Arrange
        var loader = new YamlSchemaLoader();
        var table = new DataTable("TestTable");
        
        // YAML with invalid type modifier
        var yamlWithInvalidModifier = @"
Table: TestTable
Columns:
  Id:
    Type: Int32
    TypeModifier: InvalidModifier
";
        
        // Act & Assert
        var exception = Assert.Throws<ColumnTypeException>(() => loader.LoadIntoTable(table, yamlWithInvalidModifier));
        Assert.AreEqual("Id", exception.ColumnName);
        Assert.AreEqual("InvalidModifier", exception.InvalidType);
        Assert.That(exception.Message, Does.Contain("Invalid type modifier"));
    }
    
    [Test]
    public void SchemaValidationException_ContainsErrorDetails()
    {
        // Arrange
        var loader = new YamlSchemaLoader();
        var table = new DataTable("TestTable");
        
        // YAML that violates schema (missing required Table property)
        var invalidYaml = @"
Columns:
  Id:
    Type: Int32
";
        
        // Act & Assert
        var exception = Assert.Throws<SchemaValidationException>(() => loader.LoadIntoTable(table, invalidYaml));
        Assert.IsNotEmpty(exception.Errors);
        Assert.AreEqual(invalidYaml, exception.YamlContent);
        Assert.That(exception.Message, Does.Contain("error").IgnoreCase);
    }
    
    [Test]
    public void YamlSchemaLoader_ThrowsFileNotFoundException_WhenYamlFileMissing()
    {
        // Arrange
        var loader = new YamlSchemaLoader();
        var table = new DataTable("TestTable");
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".yaml");
        
        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => 
            loader.LoadIntoTableFromFile(table, nonExistentFile));
        Assert.That(exception.Message, Does.Contain(nonExistentFile));
    }
    
    [Test]
    public void ColumnTypeException_ProvidesValidTypeAlternatives()
    {
        // Arrange & Act
        var exception = new ColumnTypeException("TestColumn", "BadType");
        
        // Assert
        Assert.AreEqual("TestColumn", exception.ColumnName);
        Assert.AreEqual("BadType", exception.InvalidType);
        Assert.That(exception.Message, Does.Contain("Int32"));
        Assert.That(exception.Message, Does.Contain("String"));
        Assert.That(exception.Message, Does.Contain("DateTime"));
    }
    
    [Test]
    public void YamlParsingException_CapturesLineAndColumnInfo()
    {
        // Arrange
        var yamlContent = "test: value";
        var lineNumber = 5;
        var columnNumber = 10;
        
        // Act
        var exception = new YamlParsingException("Parse error", yamlContent, lineNumber, columnNumber);
        
        // Assert
        Assert.AreEqual(yamlContent, exception.YamlContent);
        Assert.AreEqual(lineNumber, exception.LineNumber);
        Assert.AreEqual(columnNumber, exception.ColumnNumber);
        Assert.That(exception.Message, Does.Contain($"line {lineNumber}"));
        Assert.That(exception.Message, Does.Contain($"column {columnNumber}"));
    }
}
