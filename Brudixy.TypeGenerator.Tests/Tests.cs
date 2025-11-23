using System.Linq;
using Brudixy.TypeGenerator.Core;
using Konsarpoo.Collections;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests
{
  [TestFixture]
  public class Tests
  {
    [Test]
    public void TestDsGenerator()
    {
      var dsFile = @"Nmt.ds.brudixy.yaml";

      var dsYaml = @"
---
Table: dsNmt
CodeGenerationOptions:
  Namespace: Flexols.Data
Tables:
  - t_nmt
  - t_nmtgroup
TableOptions:
   t_nmt: 
      CodeProperty: ItemsTable
   t_nmtgroup:
      CodeProperty: GroupsTable
Relations:
  FK_NmtGroup_NmtTable: 
      ParentTable: t_nmtgroup
      ChildTable: t_nmt
      ParentKey:
        - id
      ChildKey:
        - groupid
  FK_NmtGroupTable_NmtGroupTable: 
      ParentTable: t_nmtgroup
      ChildTable: t_nmtgroup
      ParentKey:
        - id
      ChildKey:
        - parentid  
XProperties:
  CheckErrors: 
    Type: Boolean
    Value: true";

      var nmtName = "Nmt.t_nmt.dt.brudixy.yaml";
      var nmtYaml = @"
---
Table: t_nmt
CodeGenerationOptions:
  Namespace: Flexols.Data
  Class: NmtTable
  RowClass: NmtTableRow
  BaseNamespace: Flexols.Core.Common.Base.Data.BaseTables
  BaseInterfaceNamespace: Flexols.Core.Common.Base.Data.BaseTables
  BaseClass: BaseTable
  BaseRowClass: BaseTableRow
Columns:
  systemobjectid: Int32
  groupid: Int32
  supplierorderdoctypeid : Int32
  extcode: String
  fullname: String
  fixtype: Int32
  measureid: Int32
  dimsysid: Int32
  dim1: Decimal
  dim2: Decimal
  dim3: Decimal
  weight: Decimal
  weightmeasureid: Int32
  square: Decimal
  squaremeasureid: Int32
  volume: Decimal
  volumemeasureid: Int32
  fixpricesys: Int32
  price: Decimal
ColumnOptions:
  systemobjectid: 
    AllowNull: false 
    IsService: true
  extcode:  
    MaxLength: 256
  fullname: 
    MaxLength: 512  
XProperties:
  SqlViewName: 
    Type: String 
    Value: v_nmt
Indexes:
  TypePrice:
    Columns:
      - fixtype
      - price
    Unique: True
  FullNameExtCode:
    Columns:
      - fullname
      - extcode
  volumemeasureid:
    Columns:
      - volumemeasureid
    Unique: True
  supplierorderdoctypeid:
     Columns:
      - supplierorderdoctypeid";

      var nmtGroup = "Nmt.t_nmtgroup.dt.brudixy.yaml";
      var nmtGroupYaml = @"
---
Table: t_nmtgroup
CodeGenerationOptions:
  Namespace: Flexols.Data
  Class: NmtGroupTable
  RowClass: NmtGroupRow
  BaseNamespace: Flexols.Core.Common.Base.Data.BaseTables
  BaseInterfaceNamespace: Flexols.Core.Common.Base.Data.BaseTables
  BaseClass: GroupTable
  BaseRowClass: GroupTableRow
Columns:
  grouptype: Int32
  query: String
XProperties:
  SqlViewName: 
      Type: String
      Value: v_nmtgroup
";

      var data = _.Map(
        (dsFile, dsYaml),
        (nmtName, nmtYaml),
        (nmtGroup, nmtGroupYaml)
      );

      var strings = DataCodeGenerator
        .GenerateDatasetFiles("Nmt", dsFile, new FileSystemAccessMock(data), new YamlSchemaReader(), string.Empty).ToArray();

      Assert.AreEqual(2 * 2 + 1, strings.Length);
    }

    [Test]
    public void TestTableGenerator()
    {
      var yaml = @"
---
Table: GroupTable
CodeGenerationOptions:
  Namespace: Flexols.Core.Common.Base.Data.BaseTables
  BaseTableFileName: \BaseTable.st.brudixy.yaml
Columns:
  parentid: Int32
  name: String
  cmp: Complex|Brudixy.DataTable
ColumnOptions:
  name:
    MaxLength: 256
    HasIndex: true
Relations:
  FK_id_parentid:     
      ParentKey:
        - id
      ChildKey:
        - parentid";

      var name = @"GroupTable.st.brudixy.yaml";

      var data = _.Map(
        (name, yaml),
        ("BaseTable.st.brudixy.yaml", m_baseTableYaml));

      var strings = DataCodeGenerator
        .GenerateDatasetFiles("ds", name, new FileSystemAccessMock(data), new YamlSchemaReader(), string.Empty).ToArray();

      Assert.AreEqual(2, strings.Length);
    }

    private string m_baseTableYaml = @"
---
Table: BaseTable
CodeGenerationOptions:
  Namespace: Flexols.Core.Common.Base.Data.BaseTables
  Abstract: true
  ExtraUsing:
    - using System.Runtime.CompilerServices;
    - using NUnit.Framework;
PrimaryKey:
  - id
Columns:
  id: Int32
  createdt: DateTime
  creatorid: Int32
  lmdt: DateTime
  lmlogin: String
  isdeleted: Boolean
  guid: Guid
  employee_creator_name: String
  employee_lm_name: String
ColumnOptions:
  id: 
    IsUnique: true
    AllowNull: false
    XProperties:
      BinSec: 
        Type: Boolean
        Value: true
  createdt: 
    HasIndex: true
  creatorid: 
    Type: Int32
  lmlogin:
    MaxLength: 256    
  guid:    
    HasIndex: true
    IsUnique: true
  employee_lm_name:
    CodeProperty: EmployeeLmName
  XProperties:
    Width: 
      Type: Int32
    Tooltip: 
      Type: String
    Alignment: 
      Type: Int32
    Editor: 
      Type: Int32
GroupedProperties:
  RecordCreator: createdt|creatorid|employee_creator_name   
  RecordEditor: lmdt|lmlogin|employee_lm_name
GroupedPropertyOptions:
  RecordCreator:
    IsReadOnly: true
    Type: Tuple
  RecordEditor:
    Type: Tuple    
XProperties:
  TimeZone: 
    Type: String 
  CheckErrors: 
    Type: Boolean
    Value: true
";

    [Test]
    public void TestBaseTableGenerator()
    {
      var fileName = @"BaseTable.st.brudixy.yaml";

      var strings = DataCodeGenerator.GenerateDatasetFiles("ds.", fileName,
        new FileSystemAccessMock(_.Map((fileName, m_baseTableYaml))), new YamlSchemaReader(), string.Empty).ToArray();

      Assert.AreEqual(2, strings.Length);
    }

    [Test]
    public void TestUserTypesInGenerator()
    {
      var yaml = @"
---
Table: TestTuple
Columns:
  id: UserType|(int val1, int val2)
";

      var name = @"c:\test\Core\Abs\Test.st.brudixy.yaml";

      var data = _.Map((name, yaml));

      var strings = DataCodeGenerator
        .GenerateDatasetFiles("ds", name, new FileSystemAccessMock(data), new YamlSchemaReader(),"c:\\test\\Core").ToArray();

      Assert.AreEqual(2, strings.Length);
    }
    
    [Test]
    public void TestColumnTypeDef()
    {
      var yaml = @"
---
Table: TestTable
Columns:
  id: int
  sn: int? 
  name: string | 256 | Index
  expression: string | ""name + id"" 
  data: byte[] | 256
  interval: DateTime<>
  user_tuple: (int id, string name)
  user_class: SalesInfo | Class
  complex: Brudixy.DataTable | Complex
";

      var name = @"c:\test\Core\Abs\Test.st.brudixy.yaml";

      var data = _.Map((name, yaml));

      var strings = DataCodeGenerator
        .GenerateDatasetFiles("ds", name, new FileSystemAccessMock(data), new YamlSchemaReader(),"c:\\test\\Core").ToArray();

      Assert.AreEqual(2, strings.Length);
    }

    private class FileSystemAccessMock : Brudixy.TypeGenerator.Core.IFileSystemAccessor
    {
      private readonly Map<string, string> m_data;

      public FileSystemAccessMock(Map<string, string> data)
      {
        m_data = data;
      }

      public string GetFileContents(string path)
      {
        return m_data[path];
      }
    }
  }
}