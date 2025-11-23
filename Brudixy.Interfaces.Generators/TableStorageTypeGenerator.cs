using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Interfaces.Generators
{
	
	
    [Generator]
    public class TableStorageTypeGenerator : ISourceGenerator
    {
	    public static List<StorageType> StorageTypes => BuiltinSupportStorageTypes.StorageTypes;
	   
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
	        var sb = new StringBuilder();

	        var storageTypes = StorageTypes;
	        
	        var predefinedCount = storageTypes.Count;
	        
	        for (var index = 0; index < predefinedCount; index++)
	        {
		        var val = index + 1;
		        
		        sb.Append($"	    {storageTypes[index].EnumName} = {val}, ");
		        
		        if (storageTypes[index].ArraySupport)
		        {
			        sb.Append($"{storageTypes[index].EnumName}Array = {val * 1000}, ");
		        }

		        if (storageTypes[index].Comparable && storageTypes[index].RangeSupport)
		        {
			        sb.Append($"{storageTypes[index].EnumName}Range = {val * 10000}, ");
		        }

		        sb.AppendLine();
	        }

	        var sourceBuilder = $@"
namespace Brudixy
{{
    public enum TableStorageType 
    {{
	    Empty = 0,
	   {sb}
    }}
}}";
	        context.AddSource($"TableStorageType", SourceText.From(sourceBuilder, Encoding.UTF8));
        }
    }
}