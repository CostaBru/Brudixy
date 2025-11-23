using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Interfaces.Generators
{
	[Generator]
    public class TableStorageArrayCreateGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
	        var sb = new StringBuilder();

	        var storageTypes = TableStorageTypeGenerator.StorageTypes;
	        
	        var predefinedCount = storageTypes.Count;
	        
	        for (var index = 0; index < predefinedCount; index++)
	        {
		        if (storageTypes[index].EnumName == "Complex")
		        {
			        continue;
		        }

		        var genClassName = string.IsNullOrEmpty(storageTypes[index].CustomType) ? storageTypes[index].GenClassName : storageTypes[index].CustomType;

		        if (storageTypes[index].ArraySupport)
		        {
			        sb.AppendLine(
				        $"\t\t\tvalue[TableStorageType.{storageTypes[index].EnumName}Array] = (len) => new {genClassName}[len];");
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
    public partial class TableStorageTypeDefaults
    {{
		private static Dictionary<TableStorageType, Func<int, Array>> m_arrayMap;

		public static Dictionary<TableStorageType, Func<int, Array>> ArrayFactoryMap => m_arrayMap ??= GetArrayFactoryMap();
		
		private static Dictionary<TableStorageType, Func<int, Array>> GetArrayFactoryMap()
		{{
            var value = new Dictionary<TableStorageType, Func<int, Array>>();

{sb}

			return value;
		}}


		public static Array CreateArray(TableStorageType type, int arrayLen)
		{{
			if(ArrayFactoryMap.TryGetValue(type, out var result))
			{{
				return result(arrayLen);
			}}

			return null;
		}}
    }}
}}";
	        context.AddSource($"TableStorageArrayCreateGenerator", SourceText.From(sourceBuilder, Encoding.UTF8));
        }
    }
}