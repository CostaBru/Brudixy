using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Interfaces.Generators;

namespace Brudixy.TypeGenerator.Core
{
    public class TableCodeGenerationOptions
    {
        public string BaseNamespace { get; set; }
        public string BaseInterfaceNamespace { get; set; }
        public string BaseClass { get; set; }
        public string BaseRowClass { get; set; }
        public string Namespace { get; set; }
        
        public string InterfaceNamespace { get; set; }
        public string Class { get; set; } 
        public string RowClass { get; set; }
        public bool Abstract { get; set; }

        public bool Sealed { get; set; } = true;
        
        public string BaseTableFileName { get; set; }

        public List<string> ExtraUsing { get; set; } = new List<string>();
        
        public string AppendRowMethodName { get; set; }
        
        /// <summary>
        /// When true, generates code with #nullable enable and uses ? for nullable reference types.
        /// </summary>
        public bool NullableReferenceTypes { get; set; } = false;
    }
    
    public class DataTableObj
    {
        private IndexedDict<string, ColumnInfo> m_columnObjects = new ();
        private IndexedDict<string, GroupColumInfo> m_groupColumnObjects = new ();
        private IndexedDict<string, RowSubTypeInfo> m_rowSubTypeObjects = new ();
        private IndexedDict<string, TableProp> m_tablesObjects = new ();
        
        public List<string> Tables { get; set; } = new ();
        
        public Dictionary<string, Dictionary<object, object>> TableOptions { get; set; } = new ();

        public IndexedDict<string, TableProp> TablesObjects => m_tablesObjects;

        public bool EnforceConstraints { get; set; }
        
        private string m_baseLoadedTableName;

        public string BaseLoadedTableName => m_baseLoadedTableName ?? BaseClass;
        public string Table { get; set; }
        public string BaseNamespace => CodeGenerationOptions.BaseNamespace;
        public string BaseInterfaceNamespace  => CodeGenerationOptions.BaseInterfaceNamespace;
        public string InterfaceNamespace  => CodeGenerationOptions.InterfaceNamespace;
        public string BaseClass => CodeGenerationOptions.BaseClass;
        public bool HasBaseClass => CodeGenerationOptions.BaseClass != "DataTable";
        public bool Sealed => CodeGenerationOptions.Sealed && CodeGenerationOptions.Abstract == false;
        public string BaseRowClass => CodeGenerationOptions.BaseRowClass;
        public string Namespace => CodeGenerationOptions.Namespace;
        public string Class => CodeGenerationOptions.Class;
        public string RowClass => CodeGenerationOptions.RowClass;
        public bool Abstract => CodeGenerationOptions.Abstract;

        public TableCodeGenerationOptions CodeGenerationOptions { get; set; } = new TableCodeGenerationOptions();

        public List<string> PrimaryKey { get; set; } = new ();
        
        public Dictionary<string, XProperty> XProperties { get; set; } = new (StringComparer.OrdinalIgnoreCase);
        
        public IndexedDict<string, string> Columns { get; set; } = new (StringComparer.OrdinalIgnoreCase);
        
        public IndexedDict<string, Dictionary<object, object>> ColumnOptions { get; set; } = new (StringComparer.OrdinalIgnoreCase);
        
        public IndexedDict<string, string> GroupedProperties { get; set; } = new (StringComparer.OrdinalIgnoreCase);
        public IndexedDict<string, Dictionary<object, object>> GroupedPropertyOptions { get; set; } = new (StringComparer.OrdinalIgnoreCase);
        public IndexedDict<string, Dictionary<object, object>> RowSubTypeOptions { get; set; } = new (StringComparer.OrdinalIgnoreCase);
        
        public IndexedDict<string, string> RowSubTypes { get; set; } = new (StringComparer.OrdinalIgnoreCase);

        public IndexedDict<string, ColumnInfo> ColumnObjects => m_columnObjects;
        
        public IndexedDict<string, GroupColumInfo> GroupColumnObjects => m_groupColumnObjects;

        public Dictionary<string, ColumnInfoOverride> ColumnOverrides { get; set; } = new (StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, DataRelationObj> Relations { get; set; } = new (StringComparer.OrdinalIgnoreCase);
        
        public Dictionary<string, DataRelationObj> ParentRelations { get; set; } = new (StringComparer.OrdinalIgnoreCase);
        
        public Dictionary<string, DataRelationObj> ChildRelations { get; set; } = new (StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, Index> Indexes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, XProperty> PersistantColumnXProperties { get; set; } = new (StringComparer.OrdinalIgnoreCase);
        
        public IndexedDict<string, RowSubTypeInfo> RowSubTypesObjects => m_rowSubTypeObjects;
        
        public void EnsureDefaults()
        {
            EnsureCodeGenDefaults();
            
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

            foreach (var relation in Relations)
            {
                if (relation.Value.ChildTable == null)
                {
                    relation.Value.ChildTable = Table;
                    relation.Value.ParentTable = Table;
                }
            }

            foreach (var kv in Columns)
            {
                var columnObject = new ColumnInfo() { Type = kv.Value };
                
                ParseColumnType(kv, columnObject);

                columnObject.AllowNull = PrimaryKey.Contains(kv.Key) == false;
                
                m_columnObjects[kv.Key] = columnObject;
            }

            foreach (var kv in ColumnOptions)
            {
                if (kv.Key == "XProperties")
                {
                    //it is persist col props for all columns
                    continue;
                }
                
                var resultColOption = m_columnObjects.GetOrAdd(kv.Key, new ColumnInfo());
                
                resultColOption.AllowNull = ConvertToBool(kv.Value.GetOrDefault(nameof(ColumnInfo.AllowNull))) ?? PrimaryKey.Contains(kv.Key) == false;
                resultColOption.HasIndex = ConvertToBool(kv.Value.GetOrDefault(nameof(ColumnInfo.HasIndex))) ?? false;
                resultColOption.IsService = ConvertToBool(kv.Value.GetOrDefault(nameof(ColumnInfo.IsService))) ?? false;
                resultColOption.Auto = ConvertToBool(kv.Value.GetOrDefault(nameof(ColumnInfo.Auto))) ?? false;
                resultColOption.IsUnique = ConvertToBool(kv.Value.GetOrDefault(nameof(ColumnInfo.IsUnique))) ?? false;
                resultColOption.IsReadOnly = ConvertToBool(kv.Value.GetOrDefault(nameof(ColumnInfo.IsReadOnly))) ?? false;
                resultColOption.MaxLength = ConvertToUint(kv.Value.GetOrDefault(nameof(ColumnInfo.MaxLength)));
                resultColOption.DataType = (string)kv.Value.GetOrDefault(nameof(ColumnInfo.DataType));
                resultColOption.Expression = (string)kv.Value.GetOrDefault(nameof(ColumnInfo.Expression));
                resultColOption.DefaultValue = (string)kv.Value.GetOrDefault(nameof(ColumnInfo.DefaultValue));
                resultColOption.DisplayName = (string)kv.Value.GetOrDefault(nameof(ColumnInfo.DisplayName));
                resultColOption.CodeProperty = (string)kv.Value.GetOrDefault(nameof(ColumnInfo.CodeProperty));
                resultColOption.EnumType = (string)kv.Value.GetOrDefault(nameof(ColumnInfo.EnumType));

                if (kv.Value.GetOrDefault(nameof(ColumnInfo.XProperties)) is Dictionary<object, object> xProps)
                {
                    foreach (var xProp in xProps)
                    {
                        var xPropValue = xProp.Value as Dictionary<object, object>;

                        if (xPropValue == null)
                        {
                            continue;
                        }

                        resultColOption.XProperties[(string)xProp.Key] = new XProperty()
                        {
                            Type = (string)xPropValue.GetOrDefault(nameof(XProperty.Type)),
                            Value = (string)xPropValue.GetOrDefault(nameof(XProperty.Value)),
                            DataType = (string)xPropValue.GetOrDefault(nameof(XProperty.DataType)),
                            CodePropertyName = (string)xPropValue.GetOrDefault(nameof(XProperty.CodePropertyName)),
                            EnumType = (string)xPropValue.GetOrDefault(nameof(XProperty.EnumType)),
                        };
                    }
                }
            }

            if (ColumnOptions.GetOrDefault(nameof(ColumnInfo.XProperties)) is Dictionary<object, object> persistantProps)
            {
                foreach (var kv in persistantProps)
                {
                    var xPropValue = kv.Value as Dictionary<object, object>;

                    if (xPropValue == null)
                    {
                        continue;
                    }

                    PersistantColumnXProperties[(string)kv.Key] = new XProperty()
                    {
                        Type = (string)xPropValue.GetOrDefault(nameof(XProperty.Type)),
                        DataType = (string)xPropValue.GetOrDefault(nameof(XProperty.DataType)),
                        CodePropertyName = (string)xPropValue.GetOrDefault(nameof(XProperty.CodePropertyName)),
                    };
                }
            }

            foreach (var kv in GroupedProperties)
            {
                var resultGroupedProperties = m_groupColumnObjects.GetOrAdd(kv.Key, new GroupColumInfo());

                resultGroupedProperties.Name = kv.Key;
                resultGroupedProperties.Columns = kv.Value.Split('|').ToList();

                m_groupColumnObjects[kv.Key] = resultGroupedProperties;
            }
            
            foreach (var kv in GroupedPropertyOptions)
            {
                if (m_groupColumnObjects.ContainsKey(kv.Key))
                {
                    var resultGroupedProperties = m_groupColumnObjects.GetOrDefault(kv.Key);

                    resultGroupedProperties.IsReadOnly = ConvertToBool(kv.Value.GetOrDefault(nameof(GroupColumInfo.IsReadOnly))) ?? false;
                    resultGroupedProperties.StructName = (string)kv.Value.GetOrDefault(nameof(GroupColumInfo.StructName));
                    resultGroupedProperties.Type = (GroupType)Enum.Parse(typeof(GroupType), (string)kv.Value.GetOrDefault(nameof(GroupColumInfo.Type)), true);
                }
            }
            
            foreach (var kv in RowSubTypes)
            {
                var resultGroupedProperties = m_rowSubTypeObjects.GetOrAdd(kv.Key, new RowSubTypeInfo());

                resultGroupedProperties.Name = kv.Key;
                resultGroupedProperties.Expression = kv.Value;

                m_rowSubTypeObjects[kv.Key] = resultGroupedProperties;
            }
            
            foreach (var kv in RowSubTypeOptions)
            {
                if (m_rowSubTypeObjects.ContainsKey(kv.Key))
                {
                    var rowTypes = m_rowSubTypeObjects.GetOrDefault(kv.Key);
                    
                    if (kv.Value.GetOrDefault(nameof(ColumnInfo.XProperties)) is Dictionary<object, object> xProps)
                    {
                        foreach (var xProp in xProps)
                        {
                            var xPropValue = xProp.Value as Dictionary<object, object>;

                            if (xPropValue == null)
                            {
                                continue;
                            }

                            rowTypes.XProperties[(string)xProp.Key] = new XProperty()
                            {
                                Type = (string)xPropValue.GetOrDefault(nameof(XProperty.Type)),
                                Value = (string)xPropValue.GetOrDefault(nameof(XProperty.Value)),
                                DataType = (string)xPropValue.GetOrDefault(nameof(XProperty.DataType)),
                                CodePropertyName = (string)xPropValue.GetOrDefault(nameof(XProperty.CodePropertyName)),
                                EnumType = (string)xPropValue.GetOrDefault(nameof(XProperty.EnumType)),
                            };
                        }
                    }
                }
            }
        }
        
        private static void ParseColumnType(KeyValuePair<string, string> kv, ColumnInfo columnObject)
        {
            var parts = kv.Value.Split('|').Select(s => s.Trim()).ToList();

            if (parts.Count == 0)
            {
                return;
            }

            var typePart = parts[0];

            bool? nullable = typePart.EndsWith("?") ? true : null;

            typePart = typePart.TrimEnd('?');

            if (nullable == null)
            {
                nullable = typePart.EndsWith("!") ? false : null;
                typePart = typePart.TrimEnd('!');
            }

            var indexOfBraces = typePart.IndexOf("[]", StringComparison.Ordinal);

            bool isArray = indexOfBraces > 0;

            if (isArray)
            {
                typePart = typePart.Substring(0, indexOfBraces);
                columnObject.TypeModifier = "Array";
                columnObject.DataTypeIsStruct = false;
                columnObject.AllowNull = nullable ?? true;
            }
            
            var indexOfArrows = typePart.IndexOf("<>", StringComparison.Ordinal);

            bool isRange = indexOfArrows > 0;

            if (isRange)
            {
                typePart = typePart.Substring(0, indexOfArrows);
                columnObject.TypeModifier = "Range";
                columnObject.DataTypeIsStruct = false;
                columnObject.AllowNull = nullable ?? true;
            }

            string mappedType = null;
            string enumVal = null;
            if (BuiltinSupportStorageTypes.UserFriendlyAliasMapTypes.TryGetValue(typePart, out mappedType) ||
                BuiltinSupportStorageTypes.AliasMapTypes.TryGetValue(typePart, out enumVal) || 
                BuiltinSupportStorageTypes.KnownTypesToGenClassName.ContainsKey(typePart))
            {
                columnObject.Type = mappedType ?? enumVal ?? typePart;

                if (nullable.HasValue)
                {
                    columnObject.AllowNull = nullable;
                }
            }
            else
            {
                if (typePart.ElementAt(0) == '(' && typePart.ElementAt(typePart.Length - 1) == ')')
                {
                    columnObject.DataTypeIsStruct = true;
                    columnObject.AllowNull = nullable ?? false;
                }
                else
                {
                    columnObject.AllowNull = nullable;
                    if (nullable ?? false)
                    {
                        columnObject.DataTypeIsStruct = false;
                    }
                }

                columnObject.Type = "UserType";
                columnObject.DataType = typePart;
            }

            if (columnObject.Type == "String")
            {
                columnObject.DataTypeIsStruct = false;
                columnObject.AllowNull = nullable ?? true;
            }

            if (columnObject.DataTypeIsStruct && nullable.HasValue)
            {
                columnObject.AllowNull = true;
            }

            foreach (var part in parts.Skip(1))
            {
                if (isArray || isRange || columnObject.Type == "String")
                {
                    if (columnObject.MaxLength is null && int.TryParse(part, out var maxLen))
                    {
                        columnObject.MaxLength = (uint?)maxLen;
                        continue;
                    }
                }

                switch (part)
                {
                    case "Complex":
                        columnObject.TypeModifier = "Complex";
                        columnObject.DataTypeIsStruct = false;
                        columnObject.AllowNull = nullable ?? true;
                        continue;
                    case "Class":
                        columnObject.DataTypeIsStruct = false;
                        columnObject.AllowNull = nullable ?? true;
                        continue;
                    case "Index":
                        columnObject.HasIndex = true;
                        continue;
                    case "Auto":
                        columnObject.Auto = true;
                        continue;
                    case "Nullable":
                        columnObject.AllowNull = true;
                        continue;
                    case "Not null":
                        columnObject.AllowNull = false;
                        continue;
                    case "Service":
                        columnObject.IsService = true;
                        continue;
                    case "Unique":
                        columnObject.IsUnique = true;
                        continue;
                }

                if (part[0] == '\"' && part[part.Length - 1] == '\"')
                {
                    columnObject.Expression = part.Trim('\"');
                }
            }
        }

        public void EnsureCodeGenDefaults()
        {
            CodeGenerationOptions.Class = string.IsNullOrEmpty(Class) ? Table : Class;

            var tableClsName = Class;
            var tableRowClassXml = RowClass;

            CodeGenerationOptions.RowClass = string.IsNullOrEmpty(tableRowClassXml) ? $"{tableClsName}Row" : tableRowClassXml;

            var baseNamespace = BaseNamespace;
            var useDefaultInheritance = string.IsNullOrEmpty(baseNamespace);

            CodeGenerationOptions.BaseNamespace = useDefaultInheritance ? "Brudixy" : BaseNamespace;
            CodeGenerationOptions.BaseClass = string.IsNullOrEmpty(BaseClass) ? "DataTable" : BaseClass;
            CodeGenerationOptions.BaseRowClass = string.IsNullOrEmpty(BaseRowClass)
                ? useDefaultInheritance ? "DataRow" : $"{BaseClass}Row"
                : BaseRowClass;

            CodeGenerationOptions.BaseInterfaceNamespace = string.IsNullOrEmpty(BaseInterfaceNamespace)
                ? useDefaultInheritance ? "Brudixy.Interfaces" : BaseNamespace
                : BaseInterfaceNamespace;
            
            CodeGenerationOptions.InterfaceNamespace = string.IsNullOrEmpty(InterfaceNamespace) ? Namespace : InterfaceNamespace;
        }

        public static bool? ConvertToBool(object value)
        {
            if (value == null)
            {
                return null;
            }

            return (bool)Convert.ChangeType(value, typeof(bool));
        }
        
        public static uint? ConvertToUint(object value)
        {
            if (value == null)
            {
                return null;
            }

            return (uint)Convert.ChangeType(value, typeof(uint));
        }

        public void Merge(DataTableObj baseTable, string baseTableFleName)
        {
            this.m_baseLoadedTableName = baseTable.BaseClass;
            
            var resultColumns = new IndexedDict<string, ColumnInfo>();

            foreach (var kv in baseTable.ColumnObjects)
            {
                resultColumns[kv.Key] = kv.Value;
            }
            
            foreach (var kv in ColumnObjects)
            {
                resultColumns[kv.Key] = kv.Value;
            }
            
            var primaryKey = new List<string>(baseTable.PrimaryKey);
            primaryKey.AddRange(this.PrimaryKey);

            PrimaryKey = primaryKey;
            
            foreach (var kv in baseTable.Indexes)
            {
                Indexes[kv.Key] = kv.Value;
            }
         
            foreach (var kv in baseTable.Relations)
            {
                Relations[kv.Key] = kv.Value;
            }
            
            foreach (var kv in baseTable.ParentRelations)
            {
                ParentRelations[kv.Key] = kv.Value;
            }
            
            foreach (var kv in baseTable.ChildRelations)
            {
                ChildRelations[kv.Key] = kv.Value;
            }

            if (CodeGenerationOptions.BaseTableFileName == baseTableFleName)
            {
                CodeGenerationOptions.BaseClass = baseTable.Class;
                CodeGenerationOptions.BaseNamespace = baseTable.Namespace;
                CodeGenerationOptions.BaseRowClass = baseTable.RowClass;
                CodeGenerationOptions.BaseInterfaceNamespace = baseTable.InterfaceNamespace;
            }

            m_columnObjects = resultColumns;
        }
      
        public void UpdateRelations(DataTableObj dataSet)
        {
            var childRelationObjs = dataSet.Relations.Where(r => r.Value.ChildTable == Table).ToArray();
            var prentRelationObjs = dataSet.Relations.Where(r => r.Value.ParentTable == Table).ToArray();

            foreach (var rel in childRelationObjs)
            {
                ChildRelations[rel.Key] = rel.Value;
            }
                    
            foreach (var rel in prentRelationObjs)
            {
                ParentRelations[rel.Key] = rel.Value;
            }
        }
    }
}