using System;
using System.Text;
using Brudixy.Interfaces.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Generators
{
    [Generator]
    public class CreateDataItemTypeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            string GenDef(StorageType type, string className, string modifier = null, string valueValidator = "null", string nullableClass = null)
            {
                if (nullableClass != null)
                {
                    return $"  case TableStorageType.{type.EnumName}{modifier}: return allowNull ? new {nullableClass}(table, storageType, initColumnHandle, defaultNullValue, {valueValidator}) : new {className}(table, storageType, initColumnHandle, defaultNullValue, {valueValidator});	";
                }
                
                return $"  case TableStorageType.{type.EnumName}{modifier}: return new {className}(table, storageType, initColumnHandle, defaultNullValue, {valueValidator});	";
            }

            var typeSpecificCode = new StringBuilder();
            
            foreach (var type in DefStorageTypes.GetStorageTypes(context))
            {
                if (type.EnumName == "Complex")
                {
                    continue;
                }
                
                var className = type.CustomDataItemName ?? $"DataItem<{type.GenClassName}>";

                var valueValidator = type.ValueValidator ?? "null";

                string nullableClass = null;

                if (type.Struct)
                {
                    nullableClass = $"DataItem<{type.GenClassName}?>";
                }
                
                typeSpecificCode.AppendLine(GenDef(type, className, valueValidator: valueValidator, nullableClass: nullableClass));

                if (type.CustomDataItemName == null)
                {
                    if (type.ArraySupport)
                    {
                        typeSpecificCode.AppendLine(GenDef(type, $"DataItem<{type.GenClassName}[]>", "Array", valueValidator: "DefaultDataItemValueValidator.ArrayValidator"));
                    }
                
                    if (type.RangeSupport)
                    {
                        typeSpecificCode.AppendLine(GenDef(type, $"DataItem<Range<{type.GenClassName}>>", "Range"));
                    }
                }
            }

            var sourceBuilder = new StringBuilder($@"
using System;
using System.Collections;
using System.Xml;
using System.Xml.Linq;
using System.Text.Json.Nodes;

using Brudixy.Storage;
using Brudixy.Interfaces;


namespace Brudixy
{{
    partial class CoreDataTable
    {{
            [System.CodeDom.Compiler.GeneratedCodeAttribute(""Brudixy.Generators"", ""1.0"")]
            internal IDataItem CreateDataItem(CoreDataTable table, int initColumnHandle, bool allowNull)
            {{
                var colObj = table.DataColumnInfo.Columns[initColumnHandle].ColumnObj;

                TableStorageType storageType = colObj.Type;
               
                var defaultNullValue = colObj.DefaultValue;

                switch (storageType)
                {{
                    {typeSpecificCode}
                    case TableStorageType.Complex: throw new NotSupportedException(""Creation of complex type is not supported""); break;
                    default:
                         return new DataItem<object>(table, storageType, initColumnHandle, defaultNullValue);
                }}
	      }}
    }}
}}
");
	        context.AddSource($"CreateDataItemGenerator.Autogen", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }
    }
}