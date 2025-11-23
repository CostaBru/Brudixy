using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Generators
{
    [Generator]
    public class CreateRangeIndexGenerator : ISourceGenerator
    {
        
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var typeSpecificCode = new StringBuilder();
            
            foreach (var storageEnumValue in DefStorageTypes.GetStorageTypes(context))
            {
                if(string.IsNullOrEmpty(storageEnumValue.CustomDataItemName) == false)
                {
                    continue;
                }

                if (storageEnumValue.RangeSupport == false)
                {
                    continue;
                }
                
                var typeName = storageEnumValue.GenClassName;

                string constructorCodeOptional = "";

                var enumName = storageEnumValue.EnumName + "Range";

                typeSpecificCode.AppendLine(
                    $@"  case TableStorageType.{enumName}: index = new RangeIndex<{typeName}>(storageType); break;	 ");
            }

            var sourceBuilder = new StringBuilder($@"

using System;	  
using Brudixy.Index;

namespace Brudixy
{{
    partial class IndexInfo
    {{
                [System.CodeDom.Compiler.GeneratedCodeAttribute(""Brudixy.Generators"", ""1.0"")]
                private static void CreateRangeIndexes(TableStorageType storageType, int capacity, out IIndexStorage index, bool isUnique)
                {{
                    switch (storageType)
                    {{	          

                    {typeSpecificCode}  
                    
                    default:
                        throw new NotSupportedException($""Range indexing for ${{storageType}} column doesn't supported yet."");
                   }}
			  }}
     }}
}}
");
	        context.AddSource($"CreateRangeIndexes.Autogen", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }
    }
}