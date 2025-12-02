using System;
using System.IO;
using Brudixy.Exceptions;
using Brudixy.Serialization;
using NUnit.Framework;

namespace Brudixy.Tests.YamlSchemaLoading;

/// <summary>
/// Unit tests for SchemaValidator
/// Requirements: 8.2
/// </summary>
[TestFixture]
public class SchemaValidatorTests
{
    private SchemaValidator _validator;
    
    [SetUp]
    public void SetUp()
    {
        _validator = new SchemaValidator();
    }
    
    [Test]
    public void Validate_WithValidSimpleSchema_ReturnsSuccess()
    {
        // Arrange
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
  - Name: Name
    Type: String
    AllowNull: false
";
        
        // Act
        var result = _validator.Validate(yaml);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.IsEmpty(result.Errors);
    }
    
    [Test]
    public void Validate_WithValidComplexSchema_ReturnsSuccess()
    {
        // Arrange
        var yaml = @"
TableName: ComplexTable
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
PrimaryKey:
  - Id
Indexes:
  - Name: IX_Email
    Columns:
      - Email
    IsUnique: true
ColumnOptions:
  Email:
    MaxLength: 255
    IsUnique: true
XProperties:
  - Name: Description
    Type: String
    Value: Test table
";
        
        // Act
        var result = _validator.Validate(yaml);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.IsEmpty(result.Errors);
    }
    
    [Test]
    public void Validate_WithMissingTableName_ReturnsFailure()
    {
        // Arrange
        var yaml = @"
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
";
        
        // Act
        var result = _validator.Validate(yaml);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.IsNotEmpty(result.Errors);
        Assert.True(result.Errors.Any( e => e.Path.Contains("TableName") || e.Message.Contains("TableName") || e.Message.Contains("required")));
    }
    
    [Test]
    public void Validate_WithInvalidColumnType_ReturnsFailure()
    {
        // Arrange
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: InvalidType123
    AllowNull: false
";
        
        // Act
        var result = _validator.Validate(yaml);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.IsNotEmpty(result.Errors);
    }
    
    [Test]
    public void Validate_WithMalformedYaml_ReturnsFailure()
    {
        // Arrange
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: [this is not valid
";
        
        // Act
        var result = _validator.Validate(yaml);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.IsNotEmpty(result.Errors);
        Assert.True(result.Errors.Any(e => e.Kind == "YamlParsingError"));
    }
    
    [Test]
    public void Validate_WithEmptyYaml_ReturnsSuccess()
    {
        // Arrange
        var yaml = "";
        
        // Act
        var result = _validator.Validate(yaml);
        
        // Assert
        Assert.True(result.IsValid);
    }
    
    [Test]
    public void Validate_WithInvalidPrimaryKey_ReturnsFailure()
    {
        // Arrange
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
PrimaryKey: NotAnArray
";
        
        // Act
        var result = _validator.Validate(yaml);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.IsNotEmpty(result.Errors);
    }
    
    [Test]
    public void Validate_ValidationErrorDetails_ContainPathAndMessage()
    {
        // Arrange
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: InvalidType
    AllowNull: false
";
        
        // Act
        var result = _validator.Validate(yaml);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.IsNotEmpty(result.Errors);
        
        foreach (var error in result.Errors)
        {
            Assert.NotNull(error.Path);
            Assert.NotNull(error.Message);
            Assert.NotNull(error.Kind);
        }
    }
    
    [Test]
    public void ValidateFromFile_WithValidFile_ReturnsSuccess()
    {
        // Arrange
        var filePath = Path.Combine("YamlSchemaLoading", "Fixtures", "SimpleTable.yaml");
        
        // Act
        var result = _validator.ValidateFromFile(filePath);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.IsEmpty(result.Errors);
    }
    
    [Test]
    public void ValidateFromFile_WithInvalidFile_ReturnsFailure()
    {
        // Arrange
        var filePath = Path.Combine("YamlSchemaLoading", "Fixtures", "InvalidTable_InvalidType.yaml");
        
        // Act
        var result = _validator.ValidateFromFile(filePath);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.IsNotEmpty(result.Errors);
    }
    
    [Test]
    public void ValidateFromFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = "NonExistent.yaml";
        
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => _validator.ValidateFromFile(filePath));
    }
    
    [Test]
    public void Constructor_WithValidSchemaPath_CreatesValidator()
    {
        // Arrange
        var schemaPath = Path.Combine("schemas", "brudixy-table-schema.json");
        
        // Act
        var validator = new SchemaValidator(schemaPath);
        
        // Assert
        Assert.NotNull(validator);
    }
    
    [Test]
    public void Constructor_WithNonExistentSchemaPath_ThrowsSchemaNotFoundException()
    {
        // Arrange
        var schemaPath = "NonExistent.json";
        
        // Act & Assert
        Assert.Throws<SchemaNotFoundException>(() => new SchemaValidator(schemaPath));
    }
    
    [Test]
    public void Validate_WithBooleanValues_PreservesTypes()
    {
        // Arrange
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
  - Name: IsActive
    Type: Boolean
    AllowNull: false
ColumnOptions:
  IsActive:
    DefaultValue: true
";
        
        // Act
        var result = _validator.Validate(yaml);
        
        // Assert
        Assert.True(result.IsValid);
    }
    
    [Test]
    public void Validate_WithNumericValues_PreservesTypes()
    {
        // Arrange
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
ColumnOptions:
  Id:
    MaxLength: 100
    DefaultValue: 42
";
        
        // Act
        var result = _validator.Validate(yaml);
        
        // Assert
        Assert.True(result.IsValid);
    }
    
    [Test]
    public void Validate_WithRelations_ReturnsSuccess()
    {
        // Arrange
        var yaml = @"
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
        
        // Act
        var result = _validator.Validate(yaml);
        
        // Assert
        Assert.True(result.IsValid);
    }
    
    [Test]
    public void Validate_WithGroupedProperties_ReturnsSuccess()
    {
        // Arrange
        var yaml = @"
TableName: TestTable
Columns:
  - Name: FirstName
    Type: String
    AllowNull: false
  - Name: LastName
    Type: String
    AllowNull: false
GroupedProperties:
  - Name: FullName
    Columns:
      - FirstName
      - LastName
";
        
        // Act
        var result = _validator.Validate(yaml);
        
        // Assert
        Assert.True(result.IsValid);
    }
}
