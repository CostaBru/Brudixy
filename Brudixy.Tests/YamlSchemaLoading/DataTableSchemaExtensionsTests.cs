using System.IO;
using System.Linq;
using Brudixy.Serialization;
using NUnit.Framework;

namespace Brudixy.Tests.YamlSchemaLoading;

[TestFixture]
public class DataTableSchemaExtensionsTests
{
    /// <summary>
    /// Gets the path to a YAML fixture file in a cross-platform way.
    /// Uses the source directory location which is always available.
    /// </summary>
    private static string GetFixturePath(string fileName)
    {
        // Use the test assembly location to find the source directory
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        
        // Navigate up to the project root, then to the Fixtures folder
        // This works in all scenarios: IDE, dotnet test, CI/CD
        var fixturesDir = Path.Combine(testDirectory, "..", "..", "..", "YamlSchemaLoading", "Fixtures");
        var fullPath = Path.Combine(fixturesDir, fileName);
        
        if (File.Exists(fullPath))
            return Path.GetFullPath(fullPath);
        
        // Fallback: try different level of directory navigation (for different test runners)
        fixturesDir = Path.Combine(testDirectory, "..", "..", "..", "..", "Brudixy.Tests", "YamlSchemaLoading", "Fixtures");
        fullPath = Path.Combine(fixturesDir, fileName);
        
        if (File.Exists(fullPath))
            return Path.GetFullPath(fullPath);
            
        // Last resort: return a path that will fail with a clear error
        throw new FileNotFoundException($"Could not find YAML fixture file: {fileName}. Searched in: {testDirectory}/../../../YamlSchemaLoading/Fixtures/ and {testDirectory}/../../../../Brudixy.Tests/YamlSchemaLoading/Fixtures/");
    }

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
        var filePath = GetFixturePath("SimpleTable.yaml");
        
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
        var filePath = GetFixturePath("SimpleTable.yaml");
        
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
            GetFixturePath("SimpleTable.yaml")
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

    [Test]
    [Ignore("Temp")]
    public void ToYaml_WithSimpleTable_GeneratesValidYaml()
    {
        // Arrange
        var table = new DataTable("SimpleTable");
        table.AddColumn("Id", TableStorageType.Int32, auto: false, unique: false);
        table.AddColumn("Name", TableStorageType.String);
        table.SetPrimaryKeyColumn("Id");

        // Act
        var yaml = table.ToYaml();

        // Assert
        Assert.That(yaml, Does.Contain("Table: SimpleTable"));
        Assert.That(yaml, Does.Contain("Columns:"));
        Assert.That(yaml, Does.Contain("Id: Integer!")); 
        Assert.That(yaml, Does.Contain("Name: String")); 
        Assert.That(yaml, Does.Contain("PrimaryKey:"));
        Assert.That(yaml, Does.Contain("- Id"));
    }

    [Test]
    [Ignore("Temp")]
    public void ToYaml_WithComplexColumns_GeneratesCorrectTypes()
    {
        // Arrange
        var table = new DataTable("ComplexTable");
        table.AddColumn("Scores", TableStorageType.Int32, TableStorageTypeModifier.Array);
        table.AddColumn("NullableInt", TableStorageType.Int32, allowNull: true); 
        table.AddColumn("NotNullString", TableStorageType.String, allowNull: false);

        // Act
        var yaml = table.ToYaml();

        // Assert
        Assert.That(yaml, Does.Contain("Scores: Integer[]"));
        Assert.That(yaml, Does.Contain("NullableInt: Integer?"));
        Assert.That(yaml, Does.Contain("NotNullString: String!")); 
    }

    [Test]
    public void ToYaml_ExtensionsAndOptions()
    {
        // Arrange
        var table = new DataTable("OptionsTable");
        table.AddColumn("UniqueCol", TableStorageType.String, unique: true);
        table.AddColumn("AutoCol", TableStorageType.Int32, auto: true);
        table.AddColumn("ServiceCol", TableStorageType.Boolean, serviceColumn: true);
        table.AddColumn("DefaultCol", TableStorageType.String, defaultValue: "TestDefault");
        
        // Act
        var yaml = table.ToYaml();

        // Assert
        Assert.That(yaml, Does.Contain("ColumnObjects:"));
        Assert.That(yaml, Does.Contain("UniqueCol:"));
        Assert.That(yaml, Does.Contain("IsUnique: true"));
        
        Assert.That(yaml, Does.Contain("AutoCol:"));
        Assert.That(yaml, Does.Contain("Auto: true"));
        
        Assert.That(yaml, Does.Contain("ServiceCol:"));
        Assert.That(yaml, Does.Contain("IsService: true"));
        
        Assert.That(yaml, Does.Contain("DefaultCol:"));
        Assert.That(yaml, Does.Contain("DefaultValue: TestDefault"));
    }
}
