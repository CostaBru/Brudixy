namespace Brudixy.TypeGenerator.Core;

public interface ISchemaReader
{
    DataTableObj GetDataSet(string content);
    DataTableObj GetTable(string content);
}