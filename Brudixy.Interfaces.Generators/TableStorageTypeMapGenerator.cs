using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Interfaces.Generators
{
	[Generator]
    public class TableStorageTypeMapGenerator : ISourceGenerator
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
		        
		        sb.AppendLine($"\t\t\t\t\tcase TableStorageType.{storageTypes[index].EnumName}: {{ return typeof({storageTypes[index].GenClassName}); }}");

		        if (storageTypes[index].ArraySupport)
		        {
			        sb.AppendLine(
				        $"\t\t\t\t\tcase TableStorageType.{storageTypes[index].EnumName}Array:  {{ return typeof({storageTypes[index].GenClassName}[]); }}");
		        }

		        if (storageTypes[index].Comparable && storageTypes[index].RangeSupport)
		        { 
			        sb.AppendLine(
			        $"\t\t\t\t\tcase TableStorageType.{storageTypes[index].EnumName}Range:  {{ return typeof(Range<{storageTypes[index].GenClassName}>);  }}");
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
    public partial class TableStorageTypeMap
    {{
		public static Type GetType(TableStorageType type)
		{{
			switch(type)
			{{
{sb}
			}}

			return typeof(object);
		}}
    }}
}}";
	        context.AddSource($"TableStorageTypeMapGenerator", SourceText.From(sourceBuilder, Encoding.UTF8));
        }
    }
}