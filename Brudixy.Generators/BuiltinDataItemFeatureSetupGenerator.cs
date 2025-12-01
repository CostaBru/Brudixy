using System;
using System.Collections.Generic;
using System.Text;
using Brudixy.Interfaces.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Generators
{
    [Generator]
    public class BuiltinDataItemFeatureSetupGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
         

            var typeSpecificCode = new StringBuilder();
            
            foreach (var type in DefStorageTypes.GetStorageTypes(context))
            {
                if (type.EnumName == "Object" ||  type.AutoIncrementSupport == false)
                {
                    continue;
                }

                var t = type.GenClassName;

                var tab = "\t\t\t\t";
                
                typeSpecificCode.AppendLine($"{tab}DataItemFeatureSetup<{t}>.SumFuncRepository = (x, y) => ({t})(x + y);");
                typeSpecificCode.AppendLine($"{tab}DataItemFeatureSetup<{t}>.MaxFuncRepository = (x, y) => ({t})Math.Max(x, y);");

                if (type.AutoIncrementSupport)
                {
                    typeSpecificCode.AppendLine($"{tab}DataItemFeatureSetup<{t}>.IncrementFuncRepository = (x) => {{ var v = x; return ({t})(v + 1); }};");
                    typeSpecificCode.AppendLine($"{tab}DataItemFeatureSetup<{t}>.DivByIntFuncRepository = (x, l) => (System.Double)((System.Double)x / (System.Double)l);");
                }

                if (type.Struct)
                {
                    typeSpecificCode.AppendLine($"{tab}DataItemFeatureSetup<{t}?>.SumFuncRepository = (x, y) => ({t}?)((x ?? default({t})) + (y ?? default({t})));");
                    typeSpecificCode.AppendLine($"{tab}DataItemFeatureSetup<{t}?>.MaxFuncRepository = (x, y) => ({t}?)Math.Max((x ?? default({t})), (y ?? default({t})));");

                    if (type.AutoIncrementSupport)
                    {
                        typeSpecificCode.AppendLine($"{tab}DataItemFeatureSetup<{t}?>.IncrementFuncRepository = ((x) => ({t}?)( (x ?? default({t})) + 1));");
                        typeSpecificCode.AppendLine($"{tab}DataItemFeatureSetup<{t}?>.DivByIntFuncRepository = (x, l) => (System.Double)((System.Double)(x ?? default({t})) / (System.Double)l);");
                    }
                }
            }

            var sourceBuilder = new StringBuilder($@"

using System;

namespace Brudixy
{{
    partial class BuiltinDataItemFeatureSetup
    {{
            [System.CodeDom.Compiler.GeneratedCodeAttribute(""Brudixy.Generators"", ""1.0"")]
            public static void Register()
            {{
{typeSpecificCode}                          
			}}
    }}
}}
");
	        context.AddSource($"BuiltinDataItemFeatureSetup.Autogen", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }
    }
}