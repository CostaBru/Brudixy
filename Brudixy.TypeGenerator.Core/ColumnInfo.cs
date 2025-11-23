using System.Collections.Generic;

namespace Brudixy.TypeGenerator.Core
{
    public class ColumnInfo
    {
        private bool? m_allowNull = true;
        private bool? m_isReadOnly = false;
        public string Type { get; set; }
        public string TypeModifier { get; set; } = "Simple";
        public bool? IsUnique { get; set; } = false;

        public bool? IsReadOnly
        {
            get => (m_isReadOnly ?? false) || string.IsNullOrEmpty(Expression) == false;
            set => m_isReadOnly = value;
        }

        public bool? IsService { get; set; } = false;
        public bool? Auto { get; set; } = false;
        public string DefaultValue { get; set; }
        public uint? MaxLength { get; set; }
        public string DataType { get; set; }

        public bool DataTypeIsStruct { get; set; } = true;
        public string Expression { get; set; }
        public string DisplayName { get; set; }
        public bool? HasIndex { get; set; } = false;
        public string CodeProperty { get; set; }
        
        public string EnumType { get; set; }

        public bool? AllowNull
        {
            get => IsUnique ?? false ? false : m_allowNull;
            set => m_allowNull = value;
        }

        public Dictionary<string, XProperty> XProperties { get; set; } = new ();
    }
}