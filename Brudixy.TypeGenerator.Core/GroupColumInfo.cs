using System.Collections.Generic;

namespace Brudixy.TypeGenerator.Core
{
    public class GroupColumInfo
    {
        public string Name { get; set; }
        public List<string> Columns { get; set; } = new();
        public bool IsReadOnly { get; set; }
        public string StructName { get; set; }
        public GroupType Type { get; set; } = GroupType.Tuple;
    }

    public enum GroupType
    {
        Tuple,
        NewStruct
    }
}