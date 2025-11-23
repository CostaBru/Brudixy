using System.Text.Json.Nodes;

namespace Brudixy.Interfaces
{
    public interface IJsonSerializable
    {
        JElement ToJson();
        void FromJson(JElement element);
    }
}