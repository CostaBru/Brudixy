using System.Collections.Generic;

namespace Brudixy.TypeGenerator.Core
{
    public class Index
    {
        public List<string> Columns { get; set; } = new();
        
        public bool Unique { get; set; }
    }
}