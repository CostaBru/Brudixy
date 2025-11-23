using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Generators
{
	[Generator]
	public class ValueArrayStorageTypeGenerator : ISourceGenerator
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

				if (storageEnumValue.ArraySupport == false)
				{
					continue;
				}

				bool isHasCloneMethod = storageEnumValue.GenerateCtr;
				bool isHasConstructor = storageEnumValue.GenerateCtr;
				
				var typeName = storageEnumValue.GenClassName;

				string constructorCodeOptional = "";

				var enumName = storageEnumValue.EnumName + "Array";
				
				if (isHasConstructor)
				{
					constructorCodeOptional = $@"
        public   {enumName}Storage(int capacity) : base(capacity)
        {{         
        }}

        private  {enumName}Storage({enumName}Storage data) : base(data)
        {{          
        }}


        private  {enumName}Storage(Data<{typeName}[]> data) : base(data)
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
	          var result = new {enumName}Storage(this.Count);

              result.Ensure(this.Count);

			  int index = 0;
			  foreach(var v in this)
			  {{
					if(v is not null) 
					{{
						var newVal = new {typeName}[v.Length];

						Array.Copy(v, 0, newVal, 0, newVal.Length);

						result[index] = newVal;
					}}

					index++;
			  }}

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

namespace Brudixy.Storage
{{
   [System.CodeDom.Compiler.GeneratedCodeAttribute(""Brudixy.Generators"", ""1.0"")]
   sealed partial class  {enumName}Storage : Data<{typeName}[]>, ICloneable	  
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