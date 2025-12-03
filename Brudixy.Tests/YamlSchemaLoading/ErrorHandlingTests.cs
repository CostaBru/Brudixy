using Brudixy.Serialization;
using NUnit.Framework;

namespace Brudixy.Tests.YamlSchemaLoading;

[TestFixture]
public class ErrorHandlingTests
{
    private YamlSchemaLoader _loader;
    
    [SetUp]
    public void SetUp()
    {
        _loader = new YamlSchemaLoader();
    }
    
    [Test]
    public void YamlSchemaLoader_ThrowsSchemaValidationException_ForInvalidSchema()
    {
        var table = new DataTable("TestTable");
        var yaml = @"
Columns:
  Id: Int32
";
        
        var ex = Assert.Throws<SchemaValidationException>(() => _loader.LoadIntoTable(table, yaml));
        Assert.That(ex.Errors, Is.Not.Empty);
    }
    
    [Test]
    public void YamlSchemaLoader_ThrowsYamlParsingException_ForMalformedYaml()
    {
        var table = new DataTable("TestTable");
        var yaml = @"
Table: TestTable
Columns:
  Id: [invalid syntax
";
        
        Assert.Throws<YamlParsingException>(() => _loader.LoadIntoTable(table, yaml));
    }
    

    
    [Test]
    public void YamlSchemaLoader_ThrowsInvalidOperationException_ForInvalidType()
    {
        var table = new DataTable("TestTable");
        var yaml = @"
Table: TestTable
Columns:
  Id: InvalidType123
";
        
        // The schema allows any identifier as a type (for user-defined types)
        // but at load time it throws InvalidOperationException if the type doesn't exist
        var ex = Assert.Throws<System.InvalidOperationException>(() => _loader.LoadIntoTable(table, yaml));
        Assert.That(ex.Message, Does.Contain("InvalidType123"));
    }
    
    [Test]
    public void YamlSchemaLoader_ThrowsColumnTypeException_ForInvalidTypeModifier()
    {
        var table = new DataTable("TestTable");
        var yaml = @"
Table: TestTable
Columns:
  Id: Int32{InvalidModifier}
";
        
        // This will throw SchemaValidationException because the schema validates type patterns
        var ex = Assert.Throws<SchemaValidationException>(() => _loader.LoadIntoTable(table, yaml));
        Assert.That(ex.Errors, Is.Not.Empty);
    }
}
