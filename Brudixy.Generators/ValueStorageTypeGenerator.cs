using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Brudixy.Interfaces.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Generators
{
	[Generator]
	public class ValueStorageTypeGenerator : ISourceGenerator
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
				if(string.IsNullOrEmpty(storageEnumValue.CustomDataItemName) == false && storageEnumValue.EnumName != "String")
				{
					continue;
				}

				bool isHasCloneMethod = storageEnumValue.GenerateCtr;
				bool isHasConstructor = storageEnumValue.GenerateCtr;
				
				var typeName = storageEnumValue.GenClassName;
				var notNullTypeName = typeName;

				GenerateStorage(storageEnumValue,
					typeName, 
					isHasConstructor, 
					isHasCloneMethod,
					notNullTypeName, 
					false,
					out var autoIncrementOptional, 
					out var comparableOptional, 
					out var constructorCodeOptional, 
					out var compareCodeOptional, 
					out var cloneCodeOptional, 
					out var numericCodeOptional);

				var sourceBuilder = GenerateSource(storageEnumValue, typeName, false, autoIncrementOptional, comparableOptional, constructorCodeOptional, numericCodeOptional, compareCodeOptional, cloneCodeOptional);
				
				context.AddSource($"{storageEnumValue.EnumName}StorageGenerator", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));

				if (storageEnumValue.Struct)
				{
					var nullableType = typeName + "?";
					
					GenerateStorage(storageEnumValue,
						nullableType, 
						isHasConstructor, 
						isHasCloneMethod,
						notNullTypeName, 
						true,
						out autoIncrementOptional, 
						out comparableOptional, 
						out constructorCodeOptional, 
						out compareCodeOptional, 
						out cloneCodeOptional, 
						out numericCodeOptional);

					sourceBuilder = GenerateSource(storageEnumValue, nullableType, true, autoIncrementOptional, comparableOptional, constructorCodeOptional, numericCodeOptional, compareCodeOptional, cloneCodeOptional);
				
					context.AddSource($"{storageEnumValue.EnumName}NullableStorageGenerator", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
				}
			}
		}

		private static StringBuilder GenerateSource(StorageType storageEnumValue, 
			string typeName, 
			bool nullable,
			string autoIncrementOptional,
			string comparableOptional,
			string constructorCodeOptional, 
			string numericCodeOptional, 
			string compareCodeOptional,
			string cloneCodeOptional)
		{
			var nullableSuffix = nullable ? "Nullable" : string.Empty;
			
			var sourceBuilder = new StringBuilder($@"

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text.Json.Nodes;

using Konsarpoo.Collections;
using Brudixy.Expressions;

namespace Brudixy.Storage
{{
   [System.CodeDom.Compiler.GeneratedCodeAttribute(""Brudixy.Generators"", ""1.0"")]
   sealed partial class  {storageEnumValue.EnumName}{nullableSuffix}Storage : Data<{typeName}>, ICloneable {autoIncrementOptional}{comparableOptional} 	  
   {{
		{constructorCodeOptional}

        {numericCodeOptional}

        {compareCodeOptional}

        {cloneCodeOptional}
   }}
}}
");
			return sourceBuilder;
		}

		private static void GenerateStorage(
			StorageType storageEnumValue,
			string typeName,
			bool isHasConstructor,
			bool isHasCloneMethod,
			string notNullTypeName,
			bool nullable,
			out string autoIncrementOptional, 
			out string comparableOptional,
			out string constructorCodeOptional,
			out string compareCodeOptional,
			out string cloneCodeOptional,
			out string numericCodeOptional)
		{
			bool isComparable = storageEnumValue.Comparable;

			autoIncrementOptional = "";

			if (storageEnumValue.AutoIncrementSupport)
			{
				autoIncrementOptional =
					$", IAutoIncrementStorage, IAutoIncrementStorageTyped<{typeName}>, IAggregateStorage";
			}

			comparableOptional = "";

			if (isComparable)
			{
				comparableOptional = $", ICompareStorage, ICompareStorageTyped<{typeName}>";
			}

			var className = nullable ? $"{storageEnumValue.EnumName}NullableStorage" : $"{storageEnumValue.EnumName}Storage";
			
			constructorCodeOptional = "";

			if (isHasConstructor)
			{
				constructorCodeOptional = $@"
        public   {className}(int capacity) : base(capacity)
        {{         
        }}

        public  {className}({className} data) : base(data)
        {{          
        }}


        private {className}(Data<{typeName}> data) : base(data)
        {{            
        }}		
    ";
			}

			compareCodeOptional = "";

			if (isComparable && storageEnumValue.CustomComparable == false)
			{
				if (nullable)
				{
					compareCodeOptional = $@"
        public int Compare({typeName} val1, {typeName} val2)
		{{
		      return Nullable.Compare(val1, val2);
	    }}	

		public IEnumerable<int> Filter<V>(V value) where V : IComparable
        {{
			var defVal = default({typeName});

			if(value is {notNullTypeName} nv) 
			{{ 
				defVal = nv; 
			}}
			else 
			{{ 
				defVal = ({notNullTypeName})Convert.ChangeType(value, typeof({notNullTypeName}));
			}}

            //common case
			var root = this.GetRoot();

            if (root?.Storage != null)
            {{
				var cnt = root.Size;
				var items = root.Storage;

                for(int i = 0; i < cnt && i < items.Length; i++)
                {{
                    if (Nullable.Compare(items[i], defVal) == 0)
                    {{
                        yield return i;
                    }}
                }}
            }}
			else 
			{{
				var cnt = Count;

				for(int i = 0; i < cnt; i++)
                {{
                    if (Nullable.Compare(this.ValueByRef(i), defVal) == 0)
                    {{
                        yield return i;
                    }}
                }}
			}}
		}}			
    ";
				}
				else
				{
					compareCodeOptional = $@"
        public int Compare({typeName} val1, {typeName} val2)
		{{
		      return val1.CompareTo(val2);
	    }}	

		public IEnumerable<int> Filter<V>(V value) where V : IComparable
        {{
			var notNullValue = default({typeName});

			if(value is {typeName} nv) 
			{{ 
				notNullValue = nv; 
			}}
			else 
			{{ 
				notNullValue = ({typeName})Convert.ChangeType(value, typeof({typeName}));
			}}

            //common case
			var root = this.GetRoot();

            if (root?.Storage != null)
            {{
				var cnt = root.Size;
				var items = root.Storage;

                for(int i = 0; i < cnt && i < items.Length; i++)
                {{
                    if (items[i].CompareTo(notNullValue) == 0)
                    {{
                        yield return i;
                    }}
                }}
            }}
			else 
			{{
				var cnt = Count;

				for(int i = 0; i < cnt; i++)
                {{
                    if (this.ValueByRef(i).CompareTo(notNullValue) == 0)
                    {{
                        yield return i;
                    }}
                }}
			}}
		}}			
    ";
				}
			}

			cloneCodeOptional = "";

			if (isHasCloneMethod)
			{
				cloneCodeOptional = $@"

        
        public object Clone()
        {{
	          var result = new {className}(this);
        ";
				if (storageEnumValue.AutoIncrementSupport)

					cloneCodeOptional += $@"

			  result.m_max = m_max;

                         ";
				cloneCodeOptional += $@"				    

		      return result;
	    }} 		
    ";
			}

			numericCodeOptional = "";

			if (storageEnumValue.AutoIncrementSupport)
			{
				var setMax = "					            max = currentValue;";
				
				if(nullable)
				{
					setMax = "					            max = currentValue.Value;";
				}

				var maxVal = "m_max";

				numericCodeOptional = $@"

        private {notNullTypeName} m_max = default({notNullTypeName});

        public void ResetMax(Func<int, bool> checkFunc)
        {{
	        var max = {notNullTypeName}.MinValue;

		            for (int i = 0; i < Count; i++)
		            {{
			            if (checkFunc(i))
			            {{
				            var currentValue = this.ValueByRef(i);

				            if (max < currentValue)
				            {{
					            {setMax}
				            }}
			            }}
		            }}

		            m_max = max;
	     }}

        object IAutoIncrementStorage.Max
        {{
	            get
	            {{
		            return m_max;
	            }}
	            set
	            {{
		            if (value != null)
		            {{
			            m_max = Math.Max(({notNullTypeName})value, {maxVal});
		            }}
	            }}
		}}

        object IAutoIncrementStorage.NextAutoIncrementValue()
        {{
		            m_max++;
		            return m_max;
	    }}

		public {typeName} Max
        {{
		            get
		            {{
			            return m_max;
		            }}
		            set
		            {{
			            if (value != null)
			            {{
				            m_max = Math.Max(({notNullTypeName})value, {maxVal});
			            }}
		            }}
		}}

        public {typeName} NextAutoIncrementValue()
        {{
		            m_max++;
		            return m_max;
	    }}        
   ";

				numericCodeOptional += $@"

        public object GetAggregateValue(Data<int> handles, AggregateType type)
        {{
            if (handles.Count == 0)
            {{
                return null;
            }}

            switch (type)
            {{
                case AggregateType.Count:
                    {{
                        return handles.Count;
                    }}
                case AggregateType.First:
                    {{
                        return this[handles.OrderBy(c => c).First()];
                    }}
                case AggregateType.Sum:
                    {{
                        var hasData = false;";

				if (storageEnumValue.EnumName.StartsWith("U"))

					numericCodeOptional += $@"			            
                        UInt64 sum = 0;";

				else if (storageEnumValue.EnumName == "Single" || storageEnumValue.EnumName == "Double")

					numericCodeOptional += $@"
                        double sum = 0;";

				else if (storageEnumValue.EnumName == "Decimal")
					numericCodeOptional += $@"
                        decimal sum = 0;";

				else
					numericCodeOptional += $@"
                        Int64 sum = 0;";

				var sumCode = "                                sum += this.ValueByRef(record);";

				if (nullable)
				{
					sumCode = "  var val = this.ValueByRef(record);  if(val.HasValue) { sum += val.Value; }";
				}

				
				numericCodeOptional += $@"    
                        foreach (int record in handles)
                        {{
                            checked
                            {{
                                {sumCode}
                            }}
                            hasData = true;
                        }}
                        if (hasData)
                        {{
                            return sum;
                        }}

                        return null;
                    }}
                case AggregateType.Mean:
                    {{
                        var hasData = false; 
";
				if (storageEnumValue.EnumName.StartsWith("U"))
					numericCodeOptional += $@"    
                        double len = (uint)handles.Count; 
                        UInt64 sum = 0;";

				else if (storageEnumValue.EnumName == "Single" || storageEnumValue.EnumName == "Double")
					numericCodeOptional += $@"    
                        double sum = 0;
						double len = handles.Count;";

				else if (storageEnumValue.EnumName == "Decimal")
					numericCodeOptional += $@"    
                        decimal sum = 0;	
                        decimal len = handles.Count;";

				else
					numericCodeOptional += $@"    
                        Int64 sum = 0;
						double len = handles.Count;";

				var selectValueCode = "handles.Select(c => this.ValueByRef(c))";

				if (nullable)
				{
					selectValueCode = "handles.Where(c => this.ValueByRef(c).HasValue).Select(c => this.ValueByRef(c))";
				}

				var tryConvertToDb = "var val = (double)Convert.ChangeType(this.ValueByRef(record), typeof(System.Double));";

				if (nullable)
				{
					tryConvertToDb = "var recVal = this.ValueByRef(record); if(recVal is null) { continue; } var val = (double)Convert.ChangeType(recVal.Value, typeof(System.Double));";
				}
				
				numericCodeOptional += $@"    

                        foreach (int record in handles)
                        {{
                            checked
                            {{
                                {sumCode}
                            }}
                            hasData = true;
                        }}
                        if (hasData)
                        {{
                            return (double)(sum / len);
                        }}

                        return null;
                    }}
                case AggregateType.Max:
                    {{
                        return {selectValueCode}.Max();
                    }}
                case AggregateType.Min:
                    {{
                        return {selectValueCode}.Min();
                    }}
                case AggregateType.Var:
                case AggregateType.StDev:
                    {{
                        int count = 0;
                        double var = 0.0f;
                        double prec = 0.0f;
                        double dsum = 0.0f;
                        double sqrsum = 0.0f;

                        foreach (int record in handles)
                        {{
                            {tryConvertToDb}

                            dsum += val;
                            sqrsum += val * val;
                            count++;
                        }}

                        if (count > 1)
                        {{
                            var = ((double)count * sqrsum - (dsum * dsum));
                            prec = var / (dsum * dsum);

                            if ((prec < 1e-15) || (var < 0))
                            {{
                                var = 0;
                            }}
                            else
                            {{
                                var = var / (count * (count - 1));
                            }}

                            if (type == AggregateType.StDev)
                            {{
                                return Math.Sqrt(var);
                            }}

                            return var;
                        }}
                        return null;
                    }}
                case AggregateType.None:
                    {{
                        return null;
                    }}
            }}

            return null;
        }}
";
			}
		}
	}
}