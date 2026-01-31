using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Interfaces.Generators
{
	[Generator]
    public class TableStorageTypeToStringConversionGenerator : ISourceGenerator
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
		        
		        sb.AppendLine($"\t\t\t\t\tcase TableStorageType.{storageTypes[index].EnumName}: {{ var value = ({storageTypes[index].GenClassName})val; {storageTypes[index].ToStr} }}");
		        
		        if (storageTypes[index].ArraySupport)
		        {
			        if (storageTypes[index].ToStrArray != null)
			        {
				        sb.AppendLine($"\t\t\t\t\tcase TableStorageType.{storageTypes[index].EnumName}Array: {{ var value = ({storageTypes[index].GenClassName}[])val; {storageTypes[index].ToStrArray} }}");
			        }
			        else
			        {
				        sb.AppendLine($"\t\t\t\t\tcase TableStorageType.{storageTypes[index].EnumName}Array: " +
				                      $"{{ " +
				                      $"var valueArray = ({storageTypes[index].GenClassName}[])val; " +
				                      $"return string.Join(\"|\", valueArray.Select(value => {{ {storageTypes[index].ToStr} }}));" +
				                      $"}}");
			        }
		        }

		        if (storageTypes[index].Comparable && storageTypes[index].RangeSupport)
		        {
			        var funcStr = $"Func<{storageTypes[index].GenClassName}, string> valToStr = value => {{ {storageTypes[index].ToStr} }};";

			        var convertStr = "return $\"{valToStr(valueRange.Minimum)}|{valToStr(valueRange.Maximum)}\";";
			        
			        sb.AppendLine($"\t\t\t\t\tcase TableStorageType.{storageTypes[index].EnumName}Range: {{ {funcStr} var valueRange = (Range<{storageTypes[index].GenClassName}>)val; {convertStr} }}");
		        }
	        }

	        var sourceBuilder = $@"
using System;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Globalization;
using System.Text.Json.Nodes;
using Brudixy.Interfaces;
using Brudixy.Converter;

namespace Brudixy
{{
    public partial class TableStorageTypeStringConvertor 
    {{
		public static string ConvertToString(object val, TableStorageType type)
		{{
			if(val == null) return string.Empty;

			switch(type)
			{{
{sb}
			}}

			return val.ToString();
		}}
    }}
}}";
	        context.AddSource($"TableStorageTypeToStringConvertor", SourceText.From(sourceBuilder, Encoding.UTF8));
        }
    }
}