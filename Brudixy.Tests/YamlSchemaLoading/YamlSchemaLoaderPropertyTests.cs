using System.Linq;
using Brudixy.Serialization;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;

namespace Brudixy.Tests.YamlSchemaLoading;

[TestFixture]
public class YamlSchemaLoaderPropertyTests
{
    [FsCheck.NUnit.Property(MaxTest = 100)]
    public Property SchemaLoading_PreservesAllColumns()
    {
        // **Feature: yaml-schema-loading, Property 1: Schema loading preserves all columns**
        // **Validates: Requirements 1.1**
        
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 10)),
            columnCount =>
            {
                var table = new DataTable("TestTable");
                var columns = Enumerable.Range(1, columnCount)
                    .Select(i => $"  Col{i}: Int32")
                    .ToList();
                
                var yaml = $"Table: TestTable\nColumns:\n{string.Join("\n", columns)}";
                
                var loader = new YamlSchemaLoader();
                loader.LoadIntoTable(table, yaml);
                
                return table.GetColumns().Count() == columnCount;
            });
    }
}
