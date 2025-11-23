using System.Collections.Generic;

namespace Brudixy.TypeGenerator.Core
{
    public class ColumnInfoOverride
    {
        public string Type { get; set; }
        public string IsReadOnly { get; set; }
        public string IsServiceColumn { get; set; }
        public string Auto { get; set; }
        public string DefaultValue { get; set; }
        public string MaxLength { get; set; }
        public string Expression { get; set; }
        public string DisplayName { get; set; }

        public Dictionary<string, XProperty> XProperties { get; set; } = new ();
    }
}