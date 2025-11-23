using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Generators
{
    [Generator]
    public class CreateIndexGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var typeSpecificCode = new StringBuilder();
            
            foreach (var type in IndexSupportTypes.GetTypes(context))
            {
                var typeVal = type.Name;
                
                if (typeVal == "Complex")
                {
                    typeVal = "IComparable";
                }
                
                var fullTypeName = type.FullName;

                var uniqueIndex = type.UniqueIndexClassInit ?? $"new CoreStructHashIndex<{fullTypeName}>(isUnique)";

                if (string.IsNullOrEmpty(type.UniqueIndexClassInit))
                {
                    typeSpecificCode.AppendLine(
                        $@"  
                        case TableStorageType.{type.Name}: index = {uniqueIndex};  break;	 ");
                }
                else
                {
                    typeSpecificCode.AppendLine(
                        $@"  
                        case TableStorageType.{type.Name}: index = {uniqueIndex};   break;	 ");
                }
            }

            var sourceBuilder = new StringBuilder($@"

using System;	  
using Brudixy.Index;

namespace Brudixy
{{
    partial class IndexInfo
    {{
                [System.CodeDom.Compiler.GeneratedCodeAttribute(""Brudixy.Generators"", ""1.0"")]
                private static void CreateIndexes(TableStorageType storageType, int capacity, out IIndexStorage index, bool isUnique, bool hashIndex)
                {{
                    switch (storageType)
                    {{	          

                    {typeSpecificCode}  
                    
                    default:
                          index = new CoreHashIndex<IComparable>(isUnique);   break;
                   }}
			  }}
     }}
}}
");
	        context.AddSource($"CreateIndexesGenerator.Autogen", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }
    }
}