using System.IO;
using Brudixy.Serialization;
using NUnit.Framework;

namespace Brudixy.Tests.YamlSchemaLoading;

[TestFixture]
public class SchemaValidatorTests
{
    private SchemaValidator _validator;
    
    /// <summary>
    /// Gets the path to a YAML fixture file in a cross-platform way.
    /// </summary>
    private static string GetFixturePath(string fileName)
    {
        // Try output directory first (where files are copied during build via Link)
        var outputPath = Path.Combine(TestContext.CurrentContext.TestDirectory, fileName);
        if (File.Exists(outputPath))
            return outputPath;
        
        // Try with Fixtures subdirectory (in case Link didn't flatten)
        var fixturesPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", fileName);
        if (File.Exists(fixturesPath))
            return fixturesPath;
            
        // Try full path with YamlSchemaLoading (in case directory structure preserved)
        var fullPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "YamlSchemaLoading", "Fixtures", fileName);
        if (File.Exists(fullPath))
            return fullPath;
            
        // Fall back to source directory (for IDE test runs)
        var sourcePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "YamlSchemaLoading", "Fixtures", fileName);
        if (File.Exists(sourcePath))
            return Path.GetFullPath(sourcePath);
            
        // If still not found, return the expected output path (will fail with clear error)
        return outputPath;
    }
    
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
        var filePath = GetFixturePath("SimpleTable.yaml");
        
        var result = _validator.ValidateFromFile(filePath);
        
        Assert.That(result.IsValid, Is.True);
    }
    
    [Test]
    public void ValidateFromFile_WithInvalidFile_ReturnsSuccess()
    {
        var filePath = GetFixturePath("InvalidTable_InvalidType.yaml");
        
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
    
    //todo check buildtin types.
    public void Validate_WrongFields()
    {
        var yaml = @"
Table: TestTable
Columns1:
  X: Int321
  Y: Int32
  Name: String
GroupedProperties1:
  Point: X|Y
GroupedPropertyOptions2:
  Point:
    Type: Tuple
    IsReadOnly: true
";
        
        var result = _validator.Validate(yaml);
        
        Assert.That(result.IsValid, Is.False);
    }
}
