using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Generators
{
    [Generator]
    public class CreateForeignConstraintGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var typeSpecificCode = new StringBuilder();

            foreach (var type in DefStorageTypes.GetStorageTypes(context))
            {
                if (type.Struct)
                {
                    typeSpecificCode.Append(
                        $"          case TableStorageType.{type.EnumName}: return new TypedComparableForeignKeyConstraint<{type.GenClassName}>();	");

                    typeSpecificCode.AppendLine();
                }
            }

            var sourceBuilder = new StringBuilder($@"

using System;
using System.Collections;

using Brudixy.Storage;
using Brudixy.Constraints;

namespace Brudixy
{{
    partial class CoreDataTable
    {{
            [System.CodeDom.Compiler.GeneratedCodeAttribute(""Brudixy.Generators"", ""1.0"")]
            public static ForeignKeyConstraint CreateForeignKeyConstraint(TableStorageType typeCode)
            {{
	            switch (typeCode)
	            {{
                    {typeSpecificCode}
                    
                    default:
                            return new ForeignKeyConstraint();
                }}
			}}
    }}
}}
");
            context.AddSource($"CreateForeignConstraintGenerator.Autogen",
                SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }
    }
}