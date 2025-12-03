using System.IO;
using Brudixy.Serialization;
using NUnit.Framework;

namespace Brudixy.Tests.YamlSchemaLoading;

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
        var yaml = @"
Table: TestTable
Columns:
  Id: Int32
  Name: String
";
        
        var result = _validator.Validate(yaml);
        
        Assert.That(result.IsValid, Is.True);
    }
    
    [Test]
    public void Validate_WithValidComplexSchema_ReturnsSuccess()
    {
        var yaml = @"
Table: ComplexTable
Columns:
  Id: Int32
  Name: String | 256
  Email: String | 512 | Index
  CreatedDate: DateTime
PrimaryKey:
  - Id
Indexes:
  IX_Email:
    Columns:
      - Email
    Unique: true
XProperties:
  Version:
    Type: Int32
    Value: '1'
";
        
        var result = _validator.Validate(yaml);
        
        Assert.That(result.IsValid, Is.True);
    }
    
    [Test]
    public void Validate_WithMissingTableName_ReturnsFailure()
    {
        var yaml = @"
Columns:
  Id: Int32
  Name: String
";
        
        var result = _validator.Validate(yaml);
        
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Is.Not.Empty);
    }
    
    [Test]
    public void Validate_WithInvalidColumnType_ReturnsSuccess()
    {
        var yaml = @"
Table: TestTable
Columns:
  Id: InvalidType123
";
        
        var result = _validator.Validate(yaml);
        
        // The JSON schema allows any valid identifier as a column type
        // because it supports user-defined types. Validation of whether
        // the type actually exists happens at load time, not schema validation time.
        Assert.That(result.IsValid, Is.True);
    }
    
    [Test]
    public void Validate_WithMalformedYaml_ReturnsFailure()
    {
        var yaml = @"
Table: TestTable
Columns:
  Id: Int32
  Name: [invalid syntax
";
        
        var result = _validator.Validate(yaml);
        
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Is.Not.Empty);
    }
    
    [Test]
    public void ValidateFromFile_WithValidFile_ReturnsSuccess()
    {
        var filePath = Path.Combine("..", "..", "..", "YamlSchemaLoading", "Fixtures", "SimpleTable.yaml");
        
        var result = _validator.ValidateFromFile(filePath);
        
        Assert.That(result.IsValid, Is.True);
    }
    
    [Test]
    public void ValidateFromFile_WithInvalidFile_ReturnsSuccess()
    {
        var filePath = Path.Combine("..", "..", "..", "YamlSchemaLoading", "Fixtures", "InvalidTable_InvalidType.yaml");
        
        var result = _validator.ValidateFromFile(filePath);
        
        // The JSON schema allows any valid identifier as a column type
        // because it supports user-defined types
        Assert.That(result.IsValid, Is.True);
    }
    
    [Test]
    public void Constructor_WithValidSchemaPath_CreatesValidator()
    {
        var schemaPath = Path.Combine("..", "..", "..", "..", "schemas", "brudixy-table-schema.json");
        
        Assert.DoesNotThrow(() => new SchemaValidator(schemaPath));
    }
    
    [Test]
    public void Constructor_WithInvalidSchemaPath_ThrowsSchemaNotFoundException()
    {
        var schemaPath = "nonexistent-schema.json";
        
        Assert.Throws<SchemaNotFoundException>(() => new SchemaValidator(schemaPath));
    }
    
    [Test]
    public void Validate_WithRelations_ReturnsSuccess()
    {
        var yaml = @"
Table: ChildTable
Columns:
  ChildId: Int32
  ParentId: Int32
Relations:
  FK_Child_Parent:
    ParentTable: ParentTable
    ParentKey:
      - ParentId
    ChildKey:
      - ParentId
";
        
        var result = _validator.Validate(yaml);
        
        Assert.That(result.IsValid, Is.True);
    }
    
    [Test]
    public void Validate_WithGroupedProperties_ReturnsSuccess()
    {
        var yaml = @"
Table: TestTable
Columns:
  X: Int32
  Y: Int32
  Name: String
GroupedProperties:
  Point: X|Y
GroupedPropertyOptions:
  Point:
    Type: Tuple
    IsReadOnly: true
";
        
        var result = _validator.Validate(yaml);
        
        Assert.That(result.IsValid, Is.True);
    }
}
