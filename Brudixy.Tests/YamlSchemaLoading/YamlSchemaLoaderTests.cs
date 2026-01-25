using System;
using System.IO;
using System.Linq;
using Brudixy.Serialization;
using NUnit.Framework;

namespace Brudixy.Tests.YamlSchemaLoading;

[TestFixture]
public class YamlSchemaLoaderTests
{
    private YamlSchemaLoader _loader;
    
    /// <summary>
    /// Gets the path to a YAML fixture file in a cross-platform way.
    /// Uses the source directory location which is always available.
    /// </summary>
    private static string GetFixturePath(string fileName)
    {
        // Use the test assembly location to find the source directory
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        
        // Try multiple possible locations
        var possiblePaths = new[]
        {
            // From bin/Release/net8.0, go up 3 levels to Brudixy.Tests project root
            Path.Combine(testDirectory, "..", "..", "..", "YamlSchemaLoading", "Fixtures", fileName),
            // From bin/Debug/net8.0 (2 levels up)
            Path.Combine(testDirectory, "..", "..", "YamlSchemaLoading", "Fixtures", fileName),
            // Direct from test directory (in case running from project root)
            Path.Combine(testDirectory, "YamlSchemaLoading", "Fixtures", fileName),
            // From current directory
            Path.Combine(Environment.CurrentDirectory, "YamlSchemaLoading", "Fixtures", fileName),
            // Try navigating from a potential workspace root
            Path.Combine(testDirectory, "..", "..", "..", "..", "Brudixy.Tests", "YamlSchemaLoading", "Fixtures", fileName),
        };
        
        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
                return fullPath;
        }
            
        // Last resort: throw with clear error showing all paths tried
        var searchedPaths = string.Join("\n  ", possiblePaths.Select(Path.GetFullPath));
        throw new FileNotFoundException($"Could not find YAML fixture file: {fileName}.\nTest directory: {testDirectory}\nCurrent directory: {Environment.CurrentDirectory}\nSearched in:\n  {searchedPaths}");
    }
    
    [SetUp]
    public void SetUp()
    {
        _loader = new YamlSchemaLoader();
    }
    
    [Test]
    public void LoadIntoTable_WithSimpleSchema_CreatesTableStructure()
    {
        var table = new DataTable("TestTable");
        var yaml = @"
Table: SimpleTable
Columns:
  Id: Int32
  Name: String
";
        
        _loader.LoadIntoTable(table, yaml);
        
        Assert.That(table.Name, Is.EqualTo("SimpleTable"));
        Assert.That(table.GetColumns().Count(), Is.EqualTo(2));
        Assert.That(table.TryGetColumn("Id"), Is.Not.Null);
        Assert.That(table.TryGetColumn("Name"), Is.Not.Null);
    }
    
    [Test]
    public void LoadIntoTable_WithPrimaryKey_ConfiguresPrimaryKey()
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
        
        _loader.LoadIntoTable(table, yaml);
        
        var pkColumns = table.PrimaryKey.ToArray();
        Assert.That(pkColumns, Has.Length.EqualTo(1));
        Assert.That(pkColumns[0].ColumnName, Is.EqualTo("Id"));
    }
    
    [Test]
    public void LoadIntoTable_WithIndexes_CreatesIndexes()
    {
        var table = new DataTable("TestTable");
        var yaml = @"
Table: TestTable
Columns:
  Id: Int32
  Email: String
  Name: String
Indexes:
  IX_Email:
    Columns:
      - Email
    Unique: true
  IX_Name:
    Columns:
      - Name
    Unique: false
";
        
        _loader.LoadIntoTable(table, yaml);
        
        Assert.That(table.HasIndex("Email"), Is.True);
        Assert.That(table.HasIndex("Name"), Is.True);
    }
    
    [Test]
    public void LoadIntoTable_WithXProperties_AttachesExtendedProperties()
    {
        var table = new DataTable("TestTable");
        var yaml = @"
Table: TestTable
Columns:
  Id: Int32
XProperties:
  Description:
    Type: String
    Value: Test description
  Version:
    Type: Int32
    Value: '42'
";
        
        _loader.LoadIntoTable(table, yaml);
        
        Assert.That(table.GetXProperty<string>("Description"), Is.EqualTo("Test description"));
        Assert.That(table.GetXProperty<int>("Version"), Is.EqualTo(42));
    }
    
    [Test]
    public void LoadIntoTableFromFile_WithValidFile_LoadsSchema()
    {
        var table = new DataTable("TestTable");
        var filePath = GetFixturePath("SimpleTable.yaml");
        
        _loader.LoadIntoTableFromFile(table, filePath);
        
        Assert.That(table.Name, Is.EqualTo("SimpleTable"));
        Assert.That(table.GetColumns().Count(), Is.GreaterThan(0));
    }
    
    [Test]
    public void LoadIntoTableFromFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var table = new DataTable("Test");
        var filePath = "NonExistent.yaml";
        
        Assert.Throws<FileNotFoundException>(() => _loader.LoadIntoTableFromFile(table, filePath));
    }
    
    [Test]
    public void LoadAsChildTable_WithValidYaml_CreatesChildTable()
    {
        var dataset = new DataTable("Dataset");
        var yaml = @"
Table: ChildTable
Columns:
  Id: Int32
  Name: String
";
        
        var childTable = _loader.LoadAsChildTable(dataset, yaml);
        
        Assert.That(childTable, Is.Not.Null);
        Assert.That(childTable.Name, Is.EqualTo("ChildTable"));
        Assert.That(dataset.HasTable("ChildTable"), Is.True);
    }
    
    [Test]
    public void LoadMultipleTables_WithValidSchemas_CreatesAllTables()
    {
        var dataset = new DataTable("Dataset");
        var parentYaml = @"
Table: ParentTable
Columns:
  ParentId: Int32
  ParentName: String
PrimaryKey:
  - ParentId
";
        
        var childYaml = @"
Table: ChildTable
Columns:
  ChildId: Int32
  ParentId: Int32
  ChildName: String
PrimaryKey:
  - ChildId
";
        
        _loader.LoadMultipleTables(dataset, new[] { parentYaml, childYaml });
        
        Assert.That(dataset.HasTable("ParentTable"), Is.True);
        Assert.That(dataset.HasTable("ChildTable"), Is.True);
    }
    
    [Test]
    public void LoadMultipleTables_WithRelations_EstablishesRelations()
    {
        var dataset = new DataTable("Dataset");
        var parentYaml = @"
Table: ParentTable
Columns:
  ParentId: Int32
  ParentName: String
PrimaryKey:
  - ParentId
";
        
        var childYaml = @"
Table: ChildTable
Columns:
  ChildId: Int32
  ParentId: Int32
  ChildName: String
PrimaryKey:
  - ChildId
Relations:
  FK_Child_Parent:
    ParentTable: ParentTable
    ChildTable: ChildTable
    ParentKey:
      - ParentId
    ChildKey:
      - ParentId
";
        
        _loader.LoadMultipleTables(dataset, new[] { parentYaml, childYaml });
        
        var childTable = dataset.GetTable("ChildTable");
        var relations = childTable.ParentRelations.ToArray();
        Assert.That(relations, Is.Not.Empty);
    }
    
    [Test]
    public void LoadIntoTable_WithNullTable_ThrowsArgumentNullException()
    {
        var yaml = "Table: Test\nColumns:\n  Id: Int32";
        
        Assert.Throws<ArgumentNullException>(() => _loader.LoadIntoTable(null, yaml));
    }
    
    [Test]
    public void LoadIntoTable_WithEmptyYaml_ThrowsArgumentException()
    {
        var table = new DataTable("Test");
        
        Assert.Throws<ArgumentException>(() => _loader.LoadIntoTable(table, ""));
    }
}
