using System;
using System.Text;
using Brudixy.Interfaces.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Generators
{
    [Generator]
    public class CreateStorageTypeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            string GetTypeDef(StorageType type, string modifier = null, bool isStruct = false)
            {
                if (isStruct)
                {
                    return $" case TableStorageType.{type.EnumName}{modifier}: return allowNull ? new {type.EnumName}{modifier}NullableStorage(capacity) : new {type.EnumName}{modifier}Storage(capacity);";
                }
                return $" case TableStorageType.{type.EnumName}{modifier}: return new {type.EnumName}{modifier}Storage(capacity);";
            }

            var typeSpecificCode = new StringBuilder();
            
            foreach (var type in DefStorageTypes.GetStorageTypes(context))
            {
                if(string.IsNullOrEmpty(type.CustomDataItemName) == false)
                {
                    continue;
                }

                if (type.EnumName == "Object")
                {
                    continue;
                }
                
                typeSpecificCode.AppendLine(GetTypeDef(type, isStruct: type.Struct));

                if (type.ArraySupport)
                {
                    typeSpecificCode.AppendLine(GetTypeDef(type, "Array"));
                }
                
                if (type.RangeSupport)
                {
                    typeSpecificCode.AppendLine(GetTypeDef(type, "Range"));
                }
            }

            var sourceBuilder = new StringBuilder($@"

using System;
using System.Collections;

using Brudixy.Storage;
using Brudixy.Interfaces;

namespace Brudixy
{{
    partial class CoreDataTable
    {{
            [System.CodeDom.Compiler.GeneratedCodeAttribute(""Brudixy.Generators"", ""1.0"")]
            public static IList CreateStorage(TableStorageType typeCode, int capacity, bool allowNull)
            {{
	            switch (typeCode)
	            {{

                    {typeSpecificCode}                          
                    
                    default:
                            return new ObjectStorage(TypeCode.Object, capacity);
                }}
			}}
    }}
}}
");
	        context.AddSource($"CreateStorageGenerator.Autogen", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }
    }
}