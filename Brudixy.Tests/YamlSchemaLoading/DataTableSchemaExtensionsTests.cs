using System.IO;
using System.Linq;
using Brudixy.Serialization;
using NUnit.Framework;

namespace Brudixy.Tests.YamlSchemaLoading;

[TestFixture]
public class DataTableSchemaExtensionsTests
{
    [Test]
    public void LoadSchemaFromYaml_WithValidYaml_LoadsSchema()
    {
        var table = new DataTable("TestTable");
        var yaml = @"
Table: TestTable
Columns:
  Id: Int32
  Name: String
PrimaryKey:
  - Id
";
        
        table.LoadSchemaFromYaml(yaml);
        
        Assert.That(table.Name, Is.EqualTo("TestTable"));
        Assert.That(table.GetColumns().Count(), Is.EqualTo(2));
        Assert.That(table.PrimaryKey.Count(), Is.EqualTo(1));
    }
    
    [Test]
    public void LoadSchemaFromYamlFile_WithValidFile_LoadsSchema()
    {
        var table = new DataTable("TestTable");
        var filePath = Path.Combine("..", "..", "..", "YamlSchemaLoading", "Fixtures", "SimpleTable.yaml");
        
        table.LoadSchemaFromYamlFile(filePath);
        
        Assert.That(table.Name, Is.EqualTo("SimpleTable"));
        Assert.That(table.GetColumns().Count(), Is.GreaterThan(0));
    }
    
    [Test]
    public void LoadChildTableFromYaml_WithValidYaml_CreatesChildTable()
    {
        var dataset = new DataTable("Dataset");
        var yaml = @"
Table: ChildTable
Columns:
  Id: Int32
  Name: String
";
        
        var childTable = dataset.LoadChildTableFromYaml(yaml);
        
        Assert.That(childTable, Is.Not.Null);
        Assert.That(childTable.Name, Is.EqualTo("ChildTable"));
        Assert.That(dataset.HasTable("ChildTable"), Is.True);
    }
    
    [Test]
    public void LoadChildTableFromYamlFile_WithValidFile_CreatesChildTable()
    {
        var dataset = new DataTable("Dataset");
        var filePath = Path.Combine("..", "..", "..", "YamlSchemaLoading", "Fixtures", "SimpleTable.yaml");
        
        var childTable = dataset.LoadChildTableFromYamlFile(filePath);
        
        Assert.That(childTable, Is.Not.Null);
        Assert.That(dataset.HasTable(childTable.Name), Is.True);
    }
    
    [Test]
    public void LoadMultipleChildTables_WithValidFiles_CreatesAllTables()
    {
        var dataset = new DataTable("Dataset");
        var files = new[]
        {
            Path.Combine("..", "..", "..", "YamlSchemaLoading", "Fixtures", "SimpleTable.yaml")
        };
        
        dataset.LoadMultipleChildTables(files);
        
        Assert.That(dataset.Tables.Count(), Is.GreaterThan(0));
    }
    
    [Test]
    public void LoadSchemaFromYaml_WithNullTable_ThrowsArgumentNullException()
    {
        DataTable table = null;
        var yaml = "Table: Test\nColumns:\n  Id: Int32";
        
        Assert.Throws<System.ArgumentNullException>(() => table.LoadSchemaFromYaml(yaml));
    }
    
    [Test]
    public void LoadSchemaFromYaml_WithEmptyYaml_ThrowsArgumentException()
    {
        var table = new DataTable("Test");
        
        Assert.Throws<System.ArgumentException>(() => table.LoadSchemaFromYaml(""));
    }
    
    [Test]
    public void LoadSchemaFromYamlFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var table = new DataTable("Test");
        
        Assert.Throws<FileNotFoundException>(() => table.LoadSchemaFromYamlFile("nonexistent.yaml"));
    }
}
