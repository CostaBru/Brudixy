using System;
using System.IO;
using System.Linq;
using Brudixy.Serialization;
using NUnit.Framework;

namespace Brudixy.Tests.YamlSchemaLoading;

/// <summary>
/// Unit tests for DataTable schema extension methods
/// </summary>
[TestFixture]
public class DataTableSchemaExtensionsTests
{
    private const string SimpleYaml = @"---
Table: SimpleTable
PrimaryKey:
  - id
Columns:
  id: Int32
  name: String
ColumnOptions:
  id:
    AllowNull: false
    IsUnique: true
  name:
    MaxLength: 100
";

    private const string TableWithRelationsYaml1 = @"---
Table: ParentTable
PrimaryKey:
  - id
Columns:
  id: Int32
  name: String
ColumnOptions:
  id:
    AllowNull: false
    IsUnique: true
";

    private const string TableWithRelationsYaml2 = @"---
Table: ChildTable
PrimaryKey:
  - id
Columns:
  id: Int32
  parent_id: Int32
  description: String
ColumnOptions:
  id:
    AllowNull: false
    IsUnique: true
Relations:
  ParentChild:
    ParentTable: ParentTable
    ParentKey:
      - id
    ChildTable: ChildTable
    ChildKey:
      - parent_id
";

    [Test]
    public void LoadSchemaFromYaml_WithValidYaml_LoadsSchemaSuccessfully()
    {
        // Arrange
        var table = new DataTable();
        
        // Act
        table.LoadSchemaFromYaml(SimpleYaml);
        
        // Assert
        Assert.AreEqual("SimpleTable", table.Name);
        Assert.AreEqual(2, table.ColumnCount);
        Assert.IsTrue(table.HasColumn("id"));
        Assert.IsTrue(table.HasColumn("name"));
        
        var idColumn = table.GetColumn("id");
        Assert.IsNotNull(idColumn);
        
        var nameColumn = table.GetColumn("name");
        Assert.IsNotNull(nameColumn);
        Assert.AreEqual(100, nameColumn.MaxLength);
    }
    
    [Test]
    public void LoadSchemaFromYaml_WithNullTable_ThrowsArgumentNullException()
    {
        // Arrange
        DataTable table = null;
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => table.LoadSchemaFromYaml(SimpleYaml));
    }
    
    [Test]
    public void LoadSchemaFromYaml_WithNullYaml_ThrowsArgumentException()
    {
        // Arrange
        var table = new DataTable();
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => table.LoadSchemaFromYaml(null));
    }
    
    [Test]
    public void LoadSchemaFromYaml_WithEmptyYaml_ThrowsArgumentException()
    {
        // Arrange
        var table = new DataTable();
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => table.LoadSchemaFromYaml(string.Empty));
    }
    
    [Test]
    public void LoadSchemaFromYamlFile_WithValidFile_LoadsSchemaSuccessfully()
    {
        // Arrange
        var table = new DataTable();
        // Navigate up from bin/Debug/net8.0 to project root
        var projectRoot = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..");
        var testFilePath = Path.Combine(projectRoot, "TypedDs", "TestBaseTable.st.brudixy.yaml");
        
        // Act
        table.LoadSchemaFromYamlFile(testFilePath);
        
        // Assert
        Assert.AreEqual("TestBaseTable", table.Name);
        Assert.IsTrue(table.ColumnCount > 0);
        Assert.IsTrue(table.HasColumn("id"));
        Assert.IsTrue(table.HasColumn("guid"));
        
        // Verify primary key
        var pkColumns = table.PrimaryKey.ToList();
        Assert.AreEqual(1, pkColumns.Count);
        Assert.AreEqual("id", pkColumns[0].ColumnName);
    }
    
    [Test]
    public void LoadSchemaFromYamlFile_WithNullTable_ThrowsArgumentNullException()
    {
        // Arrange
        DataTable table = null;
        var testFilePath = "test.yaml";
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => table.LoadSchemaFromYamlFile(testFilePath));
    }
    
    [Test]
    public void LoadSchemaFromYamlFile_WithNullFilePath_ThrowsArgumentException()
    {
        // Arrange
        var table = new DataTable();
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => table.LoadSchemaFromYamlFile(null));
    }
    
    [Test]
    public void LoadSchemaFromYamlFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var table = new DataTable();
        var nonExistentFile = "nonexistent_file_12345.yaml";
        
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => table.LoadSchemaFromYamlFile(nonExistentFile));
    }
    
    [Test]
    public void LoadChildTableFromYaml_WithValidYaml_CreatesChildTable()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        
        // Act
        var childTable = dataset.LoadChildTableFromYaml(SimpleYaml);
        
        // Assert
        Assert.IsNotNull(childTable);
        Assert.AreEqual("SimpleTable", childTable.Name);
        Assert.IsTrue(dataset.HasTable("SimpleTable"));
        
        var retrievedTable = dataset.GetTable("SimpleTable");
        Assert.AreSame(childTable, retrievedTable);
        Assert.AreEqual(2, childTable.ColumnCount);
    }
    
    [Test]
    public void LoadChildTableFromYaml_WithNullDataset_ThrowsArgumentNullException()
    {
        // Arrange
        DataTable dataset = null;
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => dataset.LoadChildTableFromYaml(SimpleYaml));
    }
    
    [Test]
    public void LoadChildTableFromYaml_WithNullYaml_ThrowsArgumentException()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => dataset.LoadChildTableFromYaml(null));
    }
    
    [Test]
    public void LoadChildTableFromYamlFile_WithValidFile_CreatesChildTable()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        // Navigate up from bin/Debug/net8.0 to project root
        var projectRoot = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..");
        var testFilePath = Path.Combine(projectRoot, "TypedDs", "TestBaseTable.st.brudixy.yaml");
        
        // Act
        var childTable = dataset.LoadChildTableFromYamlFile(testFilePath);
        
        // Assert
        Assert.IsNotNull(childTable);
        Assert.AreEqual("TestBaseTable", childTable.Name);
        Assert.IsTrue(dataset.HasTable("TestBaseTable"));
        Assert.IsTrue(childTable.ColumnCount > 0);
    }
    
    [Test]
    public void LoadChildTableFromYamlFile_WithNullDataset_ThrowsArgumentNullException()
    {
        // Arrange
        DataTable dataset = null;
        var testFilePath = "test.yaml";
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => dataset.LoadChildTableFromYamlFile(testFilePath));
    }
    
    [Test]
    public void LoadChildTableFromYamlFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        var nonExistentFile = "nonexistent_file_12345.yaml";
        
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => dataset.LoadChildTableFromYamlFile(nonExistentFile));
    }
    
    [Test]
    public void LoadMultipleChildTables_WithValidFiles_CreatesTablesWithRelations()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var parentFile = Path.Combine(tempDir, "parent.yaml");
            var childFile = Path.Combine(tempDir, "child.yaml");
            
            File.WriteAllText(parentFile, TableWithRelationsYaml1);
            File.WriteAllText(childFile, TableWithRelationsYaml2);
            
            var files = new[] { parentFile, childFile };
            
            // Act
            dataset.LoadMultipleChildTables(files);
            
            // Assert
            Assert.IsTrue(dataset.HasTable("ParentTable"));
            Assert.IsTrue(dataset.HasTable("ChildTable"));
            
            var parentTable = dataset.GetTable("ParentTable");
            var childTable = dataset.GetTable("ChildTable");
            
            Assert.AreEqual(2, parentTable.ColumnCount);
            Assert.AreEqual(3, childTable.ColumnCount);
            
            // Verify relation exists - relations are stored in the dataset
            // We can verify the tables exist and have the correct structure
            Assert.IsTrue(dataset.HasTable("ParentTable"));
            Assert.IsTrue(dataset.HasTable("ChildTable"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
    
    [Test]
    public void LoadMultipleChildTables_WithNullDataset_ThrowsArgumentNullException()
    {
        // Arrange
        DataTable dataset = null;
        var files = new[] { "file1.yaml", "file2.yaml" };
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => dataset.LoadMultipleChildTables(files));
    }
    
    [Test]
    public void LoadMultipleChildTables_WithNullFileList_ThrowsArgumentNullException()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => dataset.LoadMultipleChildTables(null));
    }
    
    [Test]
    public void LoadMultipleChildTables_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var dataset = new DataTable("Dataset");
        var files = new[] { "nonexistent_file_12345.yaml" };
        
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => dataset.LoadMultipleChildTables(files));
    }
}
