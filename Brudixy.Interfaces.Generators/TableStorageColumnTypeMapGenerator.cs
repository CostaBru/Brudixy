using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Interfaces.Generators
{
	[Generator]
    public class TableStorageColumnTypeMapGenerator : ISourceGenerator
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

		        sb.AppendLine(
			        $"\t\t\t\t\tvalue[typeof({genClassName})] = TableStorageType.{storageTypes[index].EnumName};");

		        if (storageTypes[index].ArraySupport)
		        {
			        sb.AppendLine(
				        $"\t\t\t\t\tvalue[typeof({genClassName}[])] = TableStorageType.{storageTypes[index].EnumName}Array;");
		        }

		        if (storageTypes[index].Comparable && storageTypes[index].RangeSupport)
		        { 
			        sb.AppendLine(
			        $"\t\t\t\t\tvalue[typeof(Range<{genClassName}>)] = TableStorageType.{storageTypes[index].EnumName}Range;");
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
    public partial class TableStorageTypeMap
    {{
		private static Dictionary<Type, TableStorageType> m_colTypeMap;

		public static Dictionary<Type, TableStorageType> ColTypeMap => m_colTypeMap ??= GetColTypeMap();
		
		private static Dictionary<Type, TableStorageType> GetColTypeMap()
		{{
            var value = new Dictionary<Type, TableStorageType>();

{sb}

			return value;
		}}


		public static TableStorageType GetColumnType(Type type)
		{{
			if(ColTypeMap.TryGetValue(type, out var result))
			{{
				return result;
			}}

			return TableStorageType.UserType;
		}}
    }}
}}";
	        context.AddSource($"TableStorageColumnTypeMapGenerator", SourceText.From(sourceBuilder, Encoding.UTF8));
        }
    }
}