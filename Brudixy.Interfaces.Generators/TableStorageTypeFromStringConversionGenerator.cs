using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Interfaces.Generators
{
	[Generator]
    public class TableStorageTypeFromStringConversionGenerator : ISourceGenerator
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
		        
		        sb.AppendLine($"\t\t\t\t\tcase TableStorageType.{storageTypes[index].EnumName}: {{ {storageTypes[index].FromStr} }}");
		        
		        if (storageTypes[index].ArraySupport)
		        {
			        if (storageTypes[index].ToStrArray != null)
			        {
				        sb.AppendLine($"\t\t\t\t\tcase TableStorageType.{storageTypes[index].EnumName}Array:  {{ {storageTypes[index].FromStrArray} }}");
			        }
			        else
			        {
				        sb.AppendLine($"\t\t\t\t\tcase TableStorageType.{storageTypes[index].EnumName}Array: " +
				                      $"{{ " +
				                      $"var valueArray = value.Split('|');" +
				                      $"return valueArray.Select(value => {{ {storageTypes[index].FromStr} }}).ToArray();" +
				                      $"}}");
			        }
		        }

		        if (storageTypes[index].Comparable && storageTypes[index].RangeSupport)
		        {
			        var funcStr = $"Func<string, {storageTypes[index].GenClassName}> strToVal = value => {{ {storageTypes[index].FromStr} }};";

			        var convertStr = $"return new Range<{storageTypes[index].GenClassName}>(strToVal(valueArray[0]), strToVal(valueArray[1]));";
			        
			        sb.AppendLine($"\t\t\t\t\tcase TableStorageType.{storageTypes[index].EnumName}Range: {{ {funcStr} var valueArray = value.Split('|'); {convertStr} }}");
		        }
	        }

	        var sourceBuilder = $@"
using System;
using System.Xml;
using System.Xml.Linq;
using System.Numerics;
using System.Linq;
using System.Globalization;
using System.Text.Json.Nodes;
using Brudixy.Interfaces;
using Brudixy.Converter;

namespace Brudixy
{{
    public partial class TableStorageTypeStringConvertor 
    {{
		public static object ConvertFromString(string value, TableStorageType type)
		{{
			 if (string.IsNullOrEmpty(value)) return null;

			switch(type)
			{{
{sb}
			}}

			return value;
		}}
    }}
}}";
	        context.AddSource($"TableStorageTypeFromStringConvertor", SourceText.From(sourceBuilder, Encoding.UTF8));
        }
    }
}