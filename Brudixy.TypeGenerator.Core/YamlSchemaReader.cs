using Brudixy.TypeGenerator.Core;
using YamlDotNet.Serialization;

namespace Brudixy.TypeGenerator;

public class YamlSchemaReader : ISchemaReader
{
    private readonly IDeserializer m_deserializer;
    public YamlSchemaReader()
    {
        m_deserializer = new DeserializerBuilder()
            .Build();
    }

    public DataTableObj GetDataSet(string content)
    {
        return m_deserializer.Deserialize<DataTableObj>(content);
    }

    public DataTableObj GetTable(string content)
    {
        var table = m_deserializer.Deserialize<DataTableObj>(content);
        table.EnsureDefaults();
        return table;
    }

    /// <summary>
    /// Gets a table from YAML content without calling EnsureDefaults, for validation purposes.
    /// </summary>
    public DataTableObj GetTableForValidation(string content, string filePath)
    {
        var table = m_deserializer.Deserialize<DataTableObj>(content);
        // Don't call EnsureDefaults() - return raw deserialized data for validation
        return table;
    }
}