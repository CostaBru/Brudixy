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
        return m_deserializer.Deserialize<DataTableObj>(content);
    }
}