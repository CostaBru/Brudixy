using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Generators
{
    [Generator]
    public class ConstructIndexGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var typeSpecificCode = new StringBuilder();
            
            foreach (var type in IndexSupportTypes.GetTypes(context))
            {
                if (type.IsStruct == false)
                {
                    continue;
                }
                
                var storageEnumValue = type.Name;

                typeSpecificCode.AppendLine(
                    $@"     case TableStorageType.{storageEnumValue}:
                                ConstructSimpleStructIndex<{type.FullName}>(table, dataItem, column, indexIndex, Indexes, columnHandle);
                                break;	 ");
            }

            var sourceBuilder = new StringBuilder($@"

using System;	  
using Brudixy.Index;

namespace Brudixy
{{
    partial class IndexInfo
    {{
               [System.CodeDom.Compiler.GeneratedCodeAttribute(""Brudixy.Generators"", ""1.0"")]
             private void ConstructStructType(CoreDataTable table, int columnHandle, TableStorageType storageType, IDataItem dataItem, string column, int indexIndex)
             {{
                    switch (storageType)
                    {{	          

                    {typeSpecificCode}  
                    
                    default:
                        throw new NotSupportedException($""Indexing for ${{storageType}} column doesn't supported yet."");
                   }}
			  }}
     }}
}}
");
	        context.AddSource($"ConstructIndexesGenerator.Autogen", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }
    }
}