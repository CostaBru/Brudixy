using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Generators
{
    [Generator]
	public class ValueRangeStorageTypeGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
		}

		public void Execute(GeneratorExecutionContext context)
		{
			
/*#if DEBUG
			if (!Debugger.IsAttached)
			{
				Debugger.Launch();

				SpinWait.SpinUntil(() => Debugger.IsAttached);
			}
#endif */
			
			foreach (var storageEnumValue in DefStorageTypes.GetStorageTypes(context))
			{
				if(string.IsNullOrEmpty(storageEnumValue.CustomDataItemName) == false)
				{
					continue;
				}

				if (storageEnumValue.RangeSupport == false)
				{
					continue;
				}

				bool isHasCloneMethod = storageEnumValue.GenerateCtr;
				bool isHasConstructor = storageEnumValue.GenerateCtr;
				
				var typeName = storageEnumValue.GenClassName;

				string constructorCodeOptional = "";

				var enumName = storageEnumValue.EnumName + "Range";
				
				if (isHasConstructor)
				{
					constructorCodeOptional = $@"
        public   {enumName}Storage(int capacity) : base(capacity)
        {{         
        }}

        private  {enumName}Storage({enumName}Storage data) : base(data)
        {{          
        }}


        private  {enumName}Storage(Data<Range<{typeName}>> data) : base(data)
        {{            
        }}		
    ";
				}

				string compareCodeOptional = "";

				string cloneCodeOptional = "";

				if (isHasCloneMethod)
				{
					cloneCodeOptional = $@"

        
        public object Clone()
        {{
	          var result = new {enumName}Storage(this);

		      return result;
	    }} 		   ";

			
				}

				var sourceBuilder = new StringBuilder($@"

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

using Konsarpoo.Collections;

using Brudixy.Expressions;
using Brudixy.Interfaces;

namespace Brudixy.Storage
{{
   [System.CodeDom.Compiler.GeneratedCodeAttribute(""Brudixy.Generators"", ""1.0"")]
   sealed partial class {enumName}Storage : Data<Range<{typeName}>>, ICloneable	  
   {{
		{constructorCodeOptional}

        {compareCodeOptional}

        {cloneCodeOptional}
   }}
}}
");
				context.AddSource($"{enumName}StorageGenerator", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
			}
		}
	}
}