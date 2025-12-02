using System;
using System.IO;
using System.Linq;
using Brudixy.Interfaces;
using Brudixy.Serialization;
using NUnit.Framework;

namespace Brudixy.Tests.YamlSchemaLoading;

/// <summary>
/// Integration tests for YAML schema loading
/// Requirements: 8.1, 8.3, 8.4, 8.5
/// </summary>
[TestFixture]
public class IntegrationTests
{
    private YamlSchemaLoader _loader;
    
    [SetUp]
    public void SetUp()
    {
        _loader = new YamlSchemaLoader();
    }
    
    #region TestBaseTable Loading Tests
    
    [Test]
    public void LoadTestBaseTable_FromTypedDs_LoadsSuccessfully()
    {
        // Arrange
        var table = new DataTable("TestBaseTable");
        var filePath = Path.Combine("TypedDs", "TestBaseTable.st.brudixy.yaml");
        
        // Act
        _loader.LoadIntoTableFromFile(table, filePath);
        
        // Assert
        Assert.AreEqual("TestBaseTable", table.Name);
        
        // Verify columns exist
        Assert.NotNull(table.TryGetColumn("id"));
        Assert.NotNull(table.TryGetColumn("createdt"));
        Assert.NotNull(table.TryGetColumn("creatorid"));
        Assert.NotNull(table.TryGetColumn("lmdt"));
        Assert.NotNull(table.TryGetColumn("lmlogin"));
        Assert.NotNull(table.TryGetColumn("isdeleted"));
        Assert.NotNull(table.TryGetColumn("guid"));
        Assert.NotNull(table.TryGetColumn("employee_creator_name"));
        Assert.NotNull(table.TryGetColumn("employee_lm_name"));
        Assert.NotNull(table.TryGetColumn("type"));
        
        // Verify primary key
        var pkColumns = table.PrimaryKey.ToArray();
        Assert.AreEqual(1, pkColumns.Length);
        Assert.AreEqual("id", pkColumns[0].ColumnName);
        
        // Verify indexes
        Assert.True(table.HasIndex("guid"));
        
        // Verify extended properties
        Assert.NotNull(table.GetXProperty<string>("TimeZone"));
        Assert.AreEqual(true, table.GetXProperty<bool>("CheckErrors"));
        
        // Verify CodeGenerationOptions are stored
    }
    
    [Test]
    public void LoadTestBaseTable_VerifyColumnProperties()
    {
        // Arrange
        var table = new DataTable("TestBaseTable");
        var filePath = Path.Combine("TypedDs", "TestBaseTable.st.brudixy.yaml");
        
        // Act
        _loader.LoadIntoTableFromFile(table, filePath);
        
        // Assert
        var idColumn = table.GetColumn("id");
        Assert.NotNull(idColumn);
        Assert.False(idColumn.AllowNull);
        
        var lmloginColumn = table.GetColumn("lmlogin");
        Assert.NotNull(lmloginColumn);
        Assert.AreEqual(256, lmloginColumn.MaxLength);
    }
    
    #endregion
    
    #region Multiple Related Tables Tests
    
    [Test]
    public void LoadMultipleRelatedTables_CreatesTablesAndRelations()
    {
        // Arrange
        var dataset = new DataTable("TestDataset");
        
        var parentYaml = @"
TableName: Customer
Columns:
  - Name: CustomerId
    Type: Int32
    AllowNull: false
  - Name: CustomerName
    Type: String
    AllowNull: false
  - Name: Email
    Type: String
    AllowNull: false
PrimaryKey:
  - CustomerId
Indexes:
  - Name: IX_Email
    Columns:
      - Email
    IsUnique: true
ColumnOptions:
  CustomerName:
    MaxLength: 200
  Email:
    MaxLength: 255
    IsUnique: true
XProperties:
  - Name: TableDescription
    Type: String
    Value: Customer master table
";
        
        var childYaml = @"
TableName: Order
Columns:
  - Name: OrderId
    Type: Int32
    AllowNull: false
  - Name: CustomerId
    Type: Int32
    AllowNull: false
  - Name: OrderDate
    Type: DateTime
    AllowNull: false
  - Name: TotalAmount
    Type: Decimal
    AllowNull: false
PrimaryKey:
  - OrderId
Relations:
  - Name: FK_Order_Customer
    ParentTable: Customer
    ParentColumns:
      - CustomerId
    ChildColumns:
      - CustomerId
ColumnOptions:
  TotalAmount:
    DefaultValue: 0
";
        
        // Act
        _loader.LoadMultipleTables(dataset, new[] { parentYaml, childYaml });
        
        // Assert
        Assert.True(dataset.HasTable("Customer"));
        Assert.True(dataset.HasTable("Order"));
        
        var customerTable = dataset.GetTable("Customer");
        var orderTable = dataset.GetTable("Order");
        
        // Verify customer table
        Assert.AreEqual(3, customerTable.GetColumns().Count());
        Assert.True(customerTable.HasIndex("Email"));
        Assert.AreEqual("Customer master table", customerTable.GetXProperty<string>("TableDescription"));
        
        // Verify order table
        Assert.AreEqual(4, orderTable.GetColumns().Count());
        
        // Verify relation
        var parentRelations = orderTable.ParentRelations.ToArray();
        Assert.IsNotEmpty(parentRelations);
        Assert.True(parentRelations.Any(r => r.RelationName == "FK_Order_Customer"));
    }
    
    [Test]
    public void LoadMultipleRelatedTables_WithThreeLevels_EstablishesAllRelations()
    {
        // Arrange
        var dataset = new DataTable("TestDataset");
        
        var level1Yaml = @"
TableName: Category
Columns:
  - Name: CategoryId
    Type: Int32
    AllowNull: false
  - Name: CategoryName
    Type: String
    AllowNull: false
PrimaryKey:
  - CategoryId
";
        
        var level2Yaml = @"
TableName: Product
Columns:
  - Name: ProductId
    Type: Int32
    AllowNull: false
  - Name: CategoryId
    Type: Int32
    AllowNull: false
  - Name: ProductName
    Type: String
    AllowNull: false
PrimaryKey:
  - ProductId
Relations:
  - Name: FK_Product_Category
    ParentTable: Category
    ParentColumns:
      - CategoryId
    ChildColumns:
      - CategoryId
";
        
        var level3Yaml = @"
TableName: OrderItem
Columns:
  - Name: OrderItemId
    Type: Int32
    AllowNull: false
  - Name: ProductId
    Type: Int32
    AllowNull: false
  - Name: Quantity
    Type: Int32
    AllowNull: false
PrimaryKey:
  - OrderItemId
Relations:
  - Name: FK_OrderItem_Product
    ParentTable: Product
    ParentColumns:
      - ProductId
    ChildColumns:
      - ProductId
";
        
        // Act
        _loader.LoadMultipleTables(dataset, new[] { level1Yaml, level2Yaml, level3Yaml });
        
        // Assert
        Assert.True(dataset.HasTable("Category"));
        Assert.True(dataset.HasTable("Product"));
        Assert.True(dataset.HasTable("OrderItem"));
        
        var productTable = dataset.GetTable("Product");
        var orderItemTable = dataset.GetTable("OrderItem");
        
        // Verify relations
        Assert.IsNotEmpty(productTable.ParentRelations.ToArray());
        Assert.IsNotEmpty(orderItemTable.ParentRelations.ToArray());
    }
    
    #endregion
    
    #region Round-Trip Tests
    
    [Test]
    public void RoundTrip_LoadSchemaVerifyStructureUseTable()
    {
        // Arrange
        var table = new DataTable("ProductTable");
        var yaml = @"
TableName: Product
Columns:
  - Name: ProductId
    Type: Int32
    AllowNull: false
  - Name: ProductName
    Type: String
    AllowNull: false
  - Name: Price
    Type: Decimal
    AllowNull: false
  - Name: InStock
    Type: Boolean
    AllowNull: false
PrimaryKey:
  - ProductId
ColumnOptions:
  ProductName:
    MaxLength: 200
  Price:
    DefaultValue: 0
  InStock:
    DefaultValue: true
";
        
        // Act - Load schema
        _loader.LoadIntoTable(table, yaml);
        
        // Assert - Verify structure
        Assert.AreEqual("Product", table.Name);
        Assert.AreEqual(4, table.GetColumns().Count());
        
        var pkColumns = table.PrimaryKey.ToArray();
        Assert.AreEqual(1, pkColumns.Length);
        Assert.AreEqual("ProductId", pkColumns[0].ColumnName);
        
        // Act - Use table (add rows)
        var row1 = table.NewRow();
        row1["ProductId"] = 1;
        row1["ProductName"] = "Widget";
        row1["Price"] = 19.99m;
        row1["InStock"] = true;
        table.AddRow(row1);
        
        var row2 = table.NewRow();
        row2["ProductId"] = 2;
        row2["ProductName"] = "Gadget";
        row2["Price"] = 29.99m;
        row2["InStock"] = false;
        table.AddRow(row2);
        
        // Assert - Verify data
        Assert.AreEqual(2, table.RowCount);
        Assert.AreEqual("Widget", table.Rows.First()["ProductName"]);
        Assert.AreEqual(29.99m, table.Rows.Skip(1).First()["Price"]);
        
        // Act - Query data
        var inStockProducts = table.Rows.Where(r => (bool)r["InStock"]).ToList();
        
        // Assert - Verify query results
        Assert.AreEqual(1, inStockProducts.Count);
        Assert.AreEqual("Widget", inStockProducts[0]["ProductName"]);
    }
    
    [Test]
    public void RoundTrip_LoadSchemaWithRelationsUseRelations()
    {
        // Arrange
        var dataset = new DataTable("TestDataset");
        
        var authorYaml = @"
TableName: Author
Columns:
  - Name: AuthorId
    Type: Int32
    AllowNull: false
  - Name: AuthorName
    Type: String
    AllowNull: false
PrimaryKey:
  - AuthorId
";
        
        var bookYaml = @"
TableName: Book
Columns:
  - Name: BookId
    Type: Int32
    AllowNull: false
  - Name: AuthorId
    Type: Int32
    AllowNull: false
  - Name: Title
    Type: String
    AllowNull: false
PrimaryKey:
  - BookId
Relations:
  - Name: FK_Book_Author
    ParentTable: Author
    ParentColumns:
      - AuthorId
    ChildColumns:
      - AuthorId
";
        
        // Act - Load schemas
        _loader.LoadMultipleTables(dataset, new[] { authorYaml, bookYaml });
        
        var authorTable = dataset.GetTable("Author");
        var bookTable = dataset.GetTable("Book");
        
        // Act - Add data
        var author1Cnt = authorTable.NewRow();
        author1Cnt["AuthorId"] = 1;
        author1Cnt["AuthorName"] = "John Doe";
        var author1 = authorTable.AddRow(author1Cnt);
        
        var author2Cnt = authorTable.NewRow();
        author2Cnt["AuthorId"] = 2;
        author2Cnt["AuthorName"] = "Jane Smith";
        var author2 = authorTable.AddRow(author2Cnt);
        
        var book1 = bookTable.NewRow();
        book1["BookId"] = 1;
        book1["AuthorId"] = 1;
        book1["Title"] = "Book One";
        bookTable.AddRow(book1);
        
        var book2 = bookTable.NewRow();
        book2["BookId"] = 2;
        book2["AuthorId"] = 1;
        book2["Title"] = "Book Two";
        bookTable.AddRow(book2);
        
        var book3 = bookTable.NewRow();
        book3["BookId"] = 3;
        book3["AuthorId"] = 2;
        book3["Title"] = "Book Three";
        bookTable.AddRow(book3);
        
        // Assert - Verify data
        Assert.AreEqual(2, authorTable.RowCount);
        Assert.AreEqual(3, bookTable.RowCount);
        
        // Act - Use relations to navigate
        var author1Books = author1.GetChildRows("FK_Book_Author").ToArray();
        var author2Books = author2.GetChildRows("FK_Book_Author").ToArray();
        
        // Assert - Verify navigation
        Assert.AreEqual(2, author1Books.Length);
        Assert.AreEqual(1, author2Books.Length);
        Assert.AreEqual("Book One", author1Books[0]["Title"]);
        Assert.AreEqual("Book Two", author1Books[1]["Title"]);
        Assert.AreEqual("Book Three", author2Books[0]["Title"]);
    }
    
    #endregion
    
    #region Constraint Enforcement Tests
    
    [Test]
    public void ConstraintEnforcement_MaxLength_EnforcedAfterLoading()
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
ColumnOptions:
  Name:
    MaxLength: 10
";
        
        _loader.LoadIntoTable(table, yaml);
        
        // Act & Assert
        var row = table.NewRow();
        row["Id"] = 1;
        
        // This should work (within limit)
        row["Name"] = "Short";
        table.AddRow(row);
        Assert.AreEqual(1, table.RowCount);
        
        // This should fail (exceeds limit)
        var row2 = table.NewRow();
        row2["Id"] = 2;
        Assert.Throws<ArgumentException>(() => row2["Name"] = "ThisIsAVeryLongNameThatExceedsTheLimit");
    }
    
    [Test]
    public void ConstraintEnforcement_UniqueConstraint_EnforcedAfterLoading()
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
ColumnOptions:
  Email:
    IsUnique: true
";
        
        _loader.LoadIntoTable(table, yaml);
        
        // Act
        var row1 = table.NewRow();
        row1["Id"] = 1;
        row1["Email"] = "test@example.com";
        table.AddRow(row1);
        
        var row2 = table.NewRow();
        row2["Id"] = 2;
        row2["Email"] = "test@example.com"; // Duplicate email
        
        // Assert
        Assert.Throws<ArgumentException>(() => table.AddRow(row2));
    }
    
    [Test]
    public void ConstraintEnforcement_PrimaryKey_EnforcedAfterLoading()
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
        
        _loader.LoadIntoTable(table, yaml);
        
        // Act
        var row1 = table.NewRow();
        row1["Id"] = 1;
        row1["Name"] = "First";
        table.AddRow(row1);
        
        var row2 = table.NewRow();
        row2["Id"] = 1; // Duplicate primary key
        row2["Name"] = "Second";
        
        // Assert
        Assert.Throws<ArgumentException>(() => table.AddRow(row2));
    }
    
    [Test]
    public void ConstraintEnforcement_DefaultValue_AppliedAfterLoading()
    {
        // Arrange
        var table = new DataTable("TestTable");
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
  - Name: Status
    Type: String
    AllowNull: false
  - Name: IsActive
    Type: Boolean
    AllowNull: false
ColumnOptions:
  Status:
    DefaultValue: Pending
  IsActive:
    DefaultValue: true
";
        
        _loader.LoadIntoTable(table, yaml);
        
        // Act
        var row = table.NewRow();
        row["Id"] = 1;
        // Don't set Status or IsActive - should use defaults
        table.AddRow(row);
        
        // Assert
        Assert.AreEqual("Pending", row["Status"]);
        Assert.AreEqual(true, row["IsActive"]);
    }
    
    [Test]
    public void ConstraintEnforcement_NotNull_EnforcedAfterLoading()
    {
        // Arrange
        var table = new DataTable("TestTable");
        var yaml = @"
TableName: TestTable
Columns:
  - Name: Id
    Type: Int32
    AllowNull: false
  - Name: RequiredField
    Type: String
    AllowNull: false
  - Name: OptionalField
    Type: String
    AllowNull: true
";
        
        _loader.LoadIntoTable(table, yaml);
        
        // Act & Assert
        var row = table.NewRow();
        row["Id"] = 1;
        row["RequiredField"] = "Value";
        row["OptionalField"] = null; // This should be OK
        table.AddRow(row);
        
        var row2 = table.NewRow();
        row2["Id"] = 2;
        row2["RequiredField"] = null; // This should fail
        
        Assert.Throws<ArgumentException>(() => table.AddRow(row2));
    }
    
    #endregion
    
    #region Complex Scenarios
    
    [Test]
    public void ComplexScenario_LoadMultipleTablesWithAllFeatures()
    {
        // Arrange
        var dataset = new DataTable("ComplexDataset");
        
        var yaml1 = @"
TableName: Department
Columns:
  - Name: DepartmentId
    Type: Int32
    AllowNull: false
  - Name: DepartmentName
    Type: String
    AllowNull: false
  - Name: Budget
    Type: Decimal
    AllowNull: false
PrimaryKey:
  - DepartmentId
Indexes:
  - Name: IX_DepartmentName
    Columns:
      - DepartmentName
    IsUnique: true
ColumnOptions:
  DepartmentName:
    MaxLength: 100
    IsUnique: true
  Budget:
    DefaultValue: 0
XProperties:
  - Name: Description
    Type: String
    Value: Department master table
";
        
        var yaml2 = @"
TableName: Employee
Columns:
  - Name: EmployeeId
    Type: Int32
    AllowNull: false
  - Name: DepartmentId
    Type: Int32
    AllowNull: false
  - Name: FirstName
    Type: String
    AllowNull: false
  - Name: LastName
    Type: String
    AllowNull: false
  - Name: HireDate
    Type: DateTime
    AllowNull: false
  - Name: Salary
    Type: Decimal
    AllowNull: false
  - Name: IsActive
    Type: Boolean
    AllowNull: false
PrimaryKey:
  - EmployeeId
Relations:
  - Name: FK_Employee_Department
    ParentTable: Department
    ParentColumns:
      - DepartmentId
    ChildColumns:
      - DepartmentId
Indexes:
  - Name: IX_Employee_Name
    Columns:
      - LastName
      - FirstName
    IsUnique: false
ColumnOptions:
  FirstName:
    MaxLength: 50
  LastName:
    MaxLength: 50
  IsActive:
    DefaultValue: true
GroupedProperties:
  - Name: FullName
    Columns:
      - FirstName
      - LastName
";
        
        // Act
        _loader.LoadMultipleTables(dataset, new[] { yaml1, yaml2 });
        
        // Assert
        var deptTable = dataset.GetTable("Department");
        var empTable = dataset.GetTable("Employee");
        
        // Verify structure
        Assert.AreEqual(3, deptTable.GetColumns().Count());
        Assert.AreEqual(7, empTable.GetColumns().Count());
        
        // Add data
        var deptCnt = deptTable.NewRow();
        deptCnt["DepartmentId"] = 1;
        deptCnt["DepartmentName"] = "Engineering";
        deptCnt["Budget"] = 1000000m;
        var dept = deptTable.AddRow(deptCnt);
        
        var emp = empTable.NewRow();
        emp["EmployeeId"] = 1;
        emp["DepartmentId"] = 1;
        emp["FirstName"] = "John";
        emp["LastName"] = "Doe";
        emp["HireDate"] = DateTime.Now;
        emp["Salary"] = 75000m;
        emp["IsActive"] = true;
        empTable.AddRow(emp);
        
        // Verify relations work
        var deptEmployees = dept.GetChildRows("FK_Employee_Department").ToArray();
        Assert.AreEqual(1, deptEmployees.Length);
        Assert.AreEqual("John", deptEmployees[0]["FirstName"]);
    }
    
    #endregion
}
