using System.Collections.Generic;

namespace Brudixy.TypeGenerator.Core
{
    public class DataSetCodeGenerationOptions
    {
        public string BaseNamespace { get; set; }
        public string BaseInterfaceNamespace { get; set; }
        public string BaseClass { get; set; }
        public string Namespace { get; set; }
        public string Class { get; set; } 
    }
    
    public class DataSetObj
    {
        private IndexedDict<string, TableProp> m_tablesObjects = new ();
        private string m_dataSet;

        public string DataSet
        {
            get => m_dataSet ?? Class;
            set => m_dataSet = value;
        }

        public string BaseNamespace => CodeGenerationOptions.BaseNamespace;
        public string BaseInterfaceNamespace => CodeGenerationOptions.BaseInterfaceNamespace;
        public string BaseClass => CodeGenerationOptions.BaseClass;
        public string Namespace => CodeGenerationOptions.Namespace;
        public string Class => CodeGenerationOptions.Class;

        public DataSetCodeGenerationOptions CodeGenerationOptions { get; set; } = new DataSetCodeGenerationOptions();
        public Dictionary<string, XProperty> XProperties { get; set; } = new ();
        
        public List<string> Tables { get; set; } = new ();
        
        public Dictionary<string, Dictionary<object, object>> TableOptions { get; set; } = new ();

        public IndexedDict<string, TableProp> TablesObjects => m_tablesObjects;

        public Dictionary<string, DataRelationObj> Relations { get; set; } = new ();
        public bool EnforceConstraints { get; set; }

        public void EnsureDefaults()
        {
            EnsureCodeGenOptions();

            foreach (var kv in Tables)
            {
                m_tablesObjects[kv] = new TableProp() { CodeProperty = kv };
            }

            foreach (var kv in TableOptions)
            {
                var tableProp = m_tablesObjects.GetOrAdd(kv.Key, new TableProp());

                tableProp.FileName = (string)kv.Value.GetOrDefault(nameof(TableProp.FileName));
                
                var codeProp = (string)kv.Value.GetOrDefault(nameof(TableProp.CodeProperty));

                if (string.IsNullOrEmpty(codeProp) == false)
                {
                    tableProp.CodeProperty = codeProp;
                }
            }
        }

        public void EnsureCodeGenOptions()
        {
            var useBaseInheritance = string.IsNullOrEmpty(BaseNamespace);

            CodeGenerationOptions.Class = string.IsNullOrEmpty(Class) ? DataSet : Class;

            CodeGenerationOptions.BaseNamespace = useBaseInheritance ? "Brudixy" : BaseNamespace;
            CodeGenerationOptions.BaseClass = string.IsNullOrEmpty(BaseClass) ? "DataTable" : BaseClass;

            CodeGenerationOptions.BaseInterfaceNamespace = string.IsNullOrEmpty(BaseInterfaceNamespace)
                ? useBaseInheritance ? "Brudixy.Interfaces" : CodeGenerationOptions.Namespace
                : BaseInterfaceNamespace;
        }
    }
}