using System;

namespace Brudixy.TypeGenerator.Core
{
    public class DataRelationObj
    {
        public string ParentTable { get; set; }

        public string ChildTable { get; set; }

        public string[] ParentKey { get; set; } = Array.Empty<string>();
        
        public string[] ChildKey { get; set; } = Array.Empty<string>();

        public string ChildProperty{ get; set; }
        
        public string ParentProperty{ get; set; }
    }
}