using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Generators
{
    [Generator]
    public class StructIndexStorageTypeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var type in IndexSupportTypes.GetTypes(context))
            {
                continue;
                
                if (string.IsNullOrEmpty(type.UniqueIndexClassInit) == false)
                {
                    continue;
                }

                if (type.IsStruct == false)
                {
                    continue;
                }

                var structOptinal = "";
                var typeName = type.Name;
                var fullTypeName = type.FullName;

           
                var sourceBuilder = new StringBuilder($@"
using System;
using System.Collections.Generic;
using System.Linq;

using Konsarpoo.Collections;

namespace Brudixy.Index
{{
        [System.CodeDom.Compiler.GeneratedCodeAttribute(""Brudixy.Generators"", ""1.0"")]
        internal sealed partial class {typeName}HashIndex : CoreStructHashIndex<{fullTypeName}>                
        {{

                public {typeName}HashIndex(bool unique) : base(unique)
                {{
                }}

                public {typeName}HashIndex(bool unique, Map<System.{typeName}, int> storage) : base(unique, storage)
                {{
                }} 
        }}
}}
");
                context.AddSource($"{typeName}HashIndex.Autogen", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
            }
        }
    }
}