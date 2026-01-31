using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Interfaces.Generators
{
	[Generator]
    public class TableStorageDeepEqualsGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
	        var sb = new StringBuilder();

	        var storageTypes = BuiltinSupportStorageTypes.StorageTypes;
	        
	        var predefinedCount = storageTypes.Count;
	        
	        for (var index = 0; index < predefinedCount; index++)
	        {
		        if (storageTypes[index].EnumName == "Complex")
		        {
			        continue;
		        }

		        var genClassName = string.IsNullOrEmpty(storageTypes[index].CustomType) ? storageTypes[index].GenClassName : storageTypes[index].CustomType;

		        var deepEquals = storageTypes[index].DeepEquals ?? "val1 == val2";

		        sb.AppendLine(
			        $"\t\t\tvalue[TableStorageType.{storageTypes[index].EnumName}] = (v1, v2) => {{ var val1 = ({genClassName})v1; var val2 = ({genClassName})v2; return {deepEquals}; }};");

		        if (storageTypes[index].ArraySupport)
		        {
			        sb.AppendLine(
				        $"\t\t\tvalue[TableStorageType.{storageTypes[index].EnumName}Array] = (v1, v2) => " +
				        $"{{ " +
				        $"var a1 = ({genClassName}[])v1; " +
				        $"var a2 = ({genClassName}[])v2; " +
				        $"return Tool.ArraysDeepEqual(a1, a2, (val1, val2) => {{ return {deepEquals}; }});" +
				        $"}};");
		        }
	        }

	        var sourceBuilder = $@"
using System;
using System.Xml;
using System.Xml.Linq;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using Brudixy.Interfaces;
using Brudixy.Converter;

namespace Brudixy
{{
    public partial class TableStorageDeepEquals
    {{
		private static Dictionary<TableStorageType, Func<object, object, bool>> m_colTypeDeepEqualsMap;

		public static Dictionary<TableStorageType, Func<object, object, bool>> ColTypeDeepEqualsMap => m_colTypeDeepEqualsMap ??= GetColTypeDeepEqualsMap();
		
		private static Dictionary<TableStorageType, Func<object, object, bool>> GetColTypeDeepEqualsMap()
		{{
            var value = new Dictionary<TableStorageType, Func<object, object, bool>>();

{sb}

			return value;
		}}


		public static bool? DeepEquals(TableStorageType type, object objValue1, object objValue2)
		{{
			if(ColTypeDeepEqualsMap.TryGetValue(type, out var result))
			{{
				return result(objValue1, objValue2);
			}}

			return null;
		}}
    }}
}}";
	        context.AddSource($"TableStorageDeepEqualsGenerator", SourceText.From(sourceBuilder, Encoding.UTF8));
        }
    }
}