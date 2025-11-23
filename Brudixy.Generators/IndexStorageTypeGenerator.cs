using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.Generators
{
    [Generator]
    public class IndexStorageTypeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var type in IndexSupportTypes.GetTypes(context))
            {
                continue;
                
                var typeName = type.Name;
                
                if(string.IsNullOrEmpty(type.IndexClassInit) == false)
                {
                    continue;
                }
                
                var binSearchIComparable = type.IsNumeric == false;

                var structInterfaceDef = string.Empty;
                var comparableTypedIntefaceDef = string.Empty; 

                var binarySearchCode = string.Empty;
                var comparableCode = string.Empty; 
                var typedNullableCode = string.Empty; 

                var typedCode= string.Empty; 

                if (typeName == "Complex")
                {
                    typeName = "IComparable";
                    comparableCode = GetComparableCode(typeName, "IComparable", true);
                }
                else 
                {
                    comparableTypedIntefaceDef = ", IIndexComparableStorageTyped<IComparable>";
                    comparableCode = GetComparableCode(typeName, "IComparable", true);
                    typedCode = GetComparableCode(typeName, typeName, false);

                    if (type.IsStruct)
                    {
                        structInterfaceDef = @$", IIndexComparableStorageStructTyped<{typeName}>";
                        typedNullableCode = GetComparableCode(typeName, typeName + "?", true);
                    }
                }

                var compareIndexesCode = string.Empty;
                
                if (type.CustomBinSearch == false)
                {
                    compareIndexesCode = $@"  

                    public static int CompareIndexValues({typeName}IndexKey indexValue1,{typeName}IndexKey indexValue2)
                    {{
                         return indexValue1.Value.CompareTo(indexValue2.Value);
                    }}";
                    
                    if (binSearchIComparable)
                    {
                        binarySearchCode = @$"                 

                    internal static int BinarySearchLeft(Data<{typeName}IndexKey> list, ref {typeName} predicate, ref int startIndex, ref int endIndex)
                    {{
                        int lo = startIndex;
                        int hi = endIndex;
                        int res = -1;

                        while (lo <= hi)
                        {{
                            int index = lo + (hi - lo >> 1);

                            var comp = list.ValueByRef(index).Value.CompareTo(predicate);

                            if (comp > 0)
                            {{
                                  hi = index - 1;
                            }}
                            else if (comp < 0)
                            {{
                                lo = index + 1;
                            }}
                            else
                            {{
                                res = index;
                                hi = index - 1;
                            }}
                        }}                       
            
                        return res;
                    }}

                    internal static int BinarySearchRight(Data<{typeName}IndexKey> list, ref {typeName} predicate, ref int startIndex, ref int endIndex)
                    {{
                        int lo = startIndex;
                        int hi = endIndex;
                        int res = -1;

                        while (lo <= hi)
                        {{
                            int index = lo + (hi - lo >> 1);

                            int comp = list.ValueByRef(index).Value.CompareTo(predicate);

                            if (comp > 0)
                            {{
                                hi = index - 1;
                            }}
                            else if (comp < 0)
                            {{
                                lo = index + 1;
                            }}
                            else
                            {{
                                res = index;
                                lo = index + 1;
                            }}    
                        }}                       
            
                        return res;
                    }}

                    internal static int BinarySearch(Data<{typeName}IndexKey> list, ref {typeName} predicate, ref int startIndex, ref int endIndex)
                    {{
                        int lo = startIndex;
                        int hi = endIndex;

                        while (lo <= hi)
                        {{
                            int index = lo + (hi - lo >> 1);

                            int order = predicate.CompareTo(list.ValueByRef(index).Value);

                            if (order == 0)
                            {{
                                return index;
                            }}

                            if (order > 0)
                            {{
                                lo = index + 1;
                            }}
                            else
                            {{
                                hi = index - 1;
                            }}
                        }}
                        return ~lo;
                    }}

                    internal int BinarySearch({typeName} value)
                    {{
                        var startIndex = 0;
                        var endIndex = m_notNullValueList.Count - 1;

                        return BinarySearch(m_notNullValueList, ref value, ref startIndex, ref endIndex);
                    }}

                    internal int BinarySearchLeft({typeName} value)
                    {{
                        var startIndex = 0;
                        var endIndex = m_notNullValueList.Count - 1;

                        return BinarySearchLeft(m_notNullValueList, ref value, ref startIndex, ref endIndex);
                    }}

                    internal int BinarySearchRight({typeName} value)
                    {{
                        var startIndex = 0;
                        var endIndex = m_notNullValueList.Count - 1;

                        return BinarySearchRight(m_notNullValueList, ref value, ref startIndex, ref endIndex);
                    }}
                  ";
                    }
                    else
                    {
                        binarySearchCode = @$"

                    internal static int BinarySearchLeft(Data<{typeName}IndexKey> list, ref {typeName} predicate, ref int startIndex, ref int endIndex)
                    {{	
                        var array = list.GetRoot()?.Storage;
                        //common case
                        if (array != null)
                        {{
                            int lo = startIndex;
                            int hi = endIndex;
                            int res = -1;

                            while (lo <= hi)
                            {{
                                int index = lo + (hi - lo >> 1);

                                if (array[index].Value > predicate)
                                {{
                                     hi = index - 1;
                                }}
                                else if (array[index].Value < predicate)
                                {{
                                     lo = index + 1;
                                }}
                                else
                                {{
                                     res = index;
                                     hi = index - 1;
                                }}                               
                            }}
                                      
                            return res;
                        }}
                        else
                        {{	 
				            int lo = startIndex;
				            int hi = endIndex;
                            int res = -1;

				            while (lo <= hi)
				            {{
					            int index = lo + (hi - lo >> 1);

                                ref var value = ref list.ValueByRef(index);

					            if (value.Value > predicate)
                                {{
                                     hi = index - 1;
                                }}
                                else if (value.Value < predicate)
                                {{
                                      lo = index + 1;
                                }}
                                else
                                {{
                                     res = index;
                                     hi = index - 1;
                                }}                               
                            }}
                                      
                            return res;
                        }}
                    }}

                    internal static int BinarySearchRight(Data<{typeName}IndexKey> list, ref {typeName} predicate, ref int startIndex, ref int endIndex)
                    {{	
                        var array = list.GetRoot()?.Storage;
                        //common case
                        if (array != null)
                        {{
                            int lo = startIndex;
                            int hi = endIndex;
                            int res = -1;

                            while (lo <= hi)
                            {{
                                int index = lo + (hi - lo >> 1);
                                
                                if (array[index].Value > predicate)
                                {{
                                    hi = index - 1;
                                }}
                                else if (array[index].Value < predicate)
                                {{
                                    lo = index + 1;
                                }}
                                else
                                {{
                                    res = index;
                                    lo = index + 1;
                                }}                         
                            }}
                            return res;
                        }}
                        else
                        {{	 
				            int lo = startIndex;
				            int hi = endIndex;
                            int res = -1;

				            while (lo <= hi)
				            {{
					            int index = lo + (hi - lo >> 1);

                                ref var value = ref list.ValueByRef(index);

                                if (value.Value > predicate)
                                {{
                                    hi = index - 1;
                                }}
                                else if (value.Value < predicate)
                                {{
                                    lo = index + 1;
                                }}
                                else
                                {{
                                    res = index;
                                    lo = index + 1;
                                }}                             
                            }}
                            return res;
                        }}
                    }}

                    internal static int BinarySearch(Data<{typeName}IndexKey> list, ref {typeName} predicate, ref int startIndex, ref int endIndex)
                    {{	
                        var array = list.GetRoot()?.Storage;
                        //common case
                        if (array != null)
                        {{
                            int lo = startIndex;
                            int hi = endIndex;

                            while (lo <= hi)
                            {{
                                int index = lo + (hi - lo >> 1);

                                int order = 0;

                                if (predicate < array[index].Value)
                                {{
                                    order = -1;
                                }}
                                else
                                {{
                                    order = predicate > array[index].Value ? 1 : 0;
                                }}

                                if (order == 0)
                                {{
                                    return index;
                                }}

                                if (order > 0)
                                {{
                                    lo = index + 1;
                                }}
                                else
                                {{
                                    hi = index - 1;
                                }}
                            }}
                            return ~lo;
                        }}
                        else
                        {{	 
				            int lo = startIndex;
				            int hi = endIndex;

				            while (lo <= hi)
				            {{
					            int index = lo + (hi - lo >> 1);

					            int order = 0;
					            ref var value = ref list.ValueByRef(index);

					            if (predicate < value.Value)
					            {{
						            order = -1;
					            }}
					            else
					            {{
						            order = predicate > value.Value ? 1 : 0;
					            }}

					            if (order == 0)
					            {{
						            return index;
					            }}

					            if (order > 0)
					            {{
						            lo = index + 1;
					            }}
					            else
					            {{
						            hi = index - 1;
					            }}
				            }}
				            return ~lo;	
                        }}
                    }}

                    internal int BinarySearch({typeName} value)
                    {{
                        var startIndex = 0;
                        var endIndex = m_notNullValueList.Count - 1;

                        return BinarySearch(m_notNullValueList, ref value, ref startIndex, ref endIndex);
                    }}

                    internal int BinarySearchLeft({typeName} value)
                    {{
                        var startIndex = 0;
                        var endIndex = m_notNullValueList.Count - 1;

                        return BinarySearchLeft(m_notNullValueList, ref value, ref startIndex, ref endIndex);
                    }}

                    internal int BinarySearchRight({typeName} value)
                    {{
                        var startIndex = 0;
                        var endIndex = m_notNullValueList.Count - 1;

                        return BinarySearchRight(m_notNullValueList, ref value, ref startIndex, ref endIndex);
                    }}   
";
                    }
                }

                var copyClone = type.OwnCopyClone ? string.Empty : $@"
                 public IIndexStorage Clone()
                {{
                    return new {typeName}Index(new Data<{typeName}IndexKey>(), new Data<int>());
                }}

                public IIndexStorage Copy()
                {{
                    return new {typeName}Index(new Data<{typeName}IndexKey>(m_notNullValueList), new Data<int>(m_nullReferencesList));
                }}";


                var sourceBuilder = new StringBuilder($@"
using System;
using System.Collections.Generic;
using System.Linq;

using Konsarpoo.Collections;

namespace Brudixy.Index
{{
        [System.CodeDom.Compiler.GeneratedCodeAttribute(""Brudixy.Generators"", ""1.0"")]
        internal sealed partial class {typeName}Index : 
                   IIndexStorage, 
                   IIndexComparableStorageTyped<System.{typeName}> 
                   {comparableTypedIntefaceDef} {structInterfaceDef}
        {{
                [System.Diagnostics.DebuggerDisplay(""{{Value}} - {{Ref}}"")]
                public struct {typeName}IndexKey : IComparable<{typeName}IndexKey>
                {{
                    public {typeName} Value;
                    public int Ref;

			        public static bool operator ==({typeName}IndexKey a, {typeName}IndexKey b)
                    {{
                        if (a.Value != b.Value || a.Ref != b.Ref)
                        {{
                            return false;
                        }}
                        
                        return true;
                    }}
                   
                    public static bool operator !=({typeName}IndexKey a, {typeName}IndexKey b)
                    {{
                        if (a.Value == b.Value && a.Ref == b.Ref)
                        {{
                            return false;
                        }}
                        
                        return true;
                    }}

                    public int CompareTo({typeName}IndexKey other)
                    {{
                        var compareTo = Value.CompareTo(other.Value);

                        if (compareTo == 0)
                        {{
                            return Ref.CompareTo(other.Ref);
                        }}

                        return compareTo;
                    }}
                }}                

                public bool IsUnique => false;

		        public void Dispose()
	            {{
	                m_notNullValueList.Dispose();
	                m_nullReferencesList.Dispose();
	            }}

                internal Data<{typeName}IndexKey> m_notNullValueList;

                {copyClone}

                public  {typeName}Index(int capacity)
                {{   
                    m_notNullValueList = new Data<{typeName}IndexKey>(capacity);

                    m_nullReferencesList = new Data<int>();
                }}

                public  {typeName}Index(Data<{typeName}IndexKey> sortedList, Data<int> nullReferencesList)
                {{                  
                    m_notNullValueList = sortedList;

                    m_nullReferencesList = nullReferencesList;
                }}           

                internal Data<{typeName}IndexKey> m_removeItems;   

                public TableStorageType StorageType {{ get {{ return TableStorageType.{type.Name}; }} }}

                internal readonly Data<int> m_nullReferencesList; 

                public IComparable GetMaxNotNullValue(Func<int, bool> validCheck)
                {{
                    var valueList = m_notNullValueList;
                   
                    for (int i = valueList.Count - 1; i >= 0; i--)
                    {{
                        ref var key = ref valueList.ValueByRef(i);

                        if (validCheck(key.Ref))
                        {{
                            return key.Value;
                        }}
                    }}

                    return default({typeName}); 
                }}

                public IComparable GetMinNotNullValue(Func<int, bool> validCheck)
                {{
                    var valueList = m_notNullValueList;

                    var count = m_notNullValueList.Count;
                  
                    for (int i = 0; i < count; i++)
                    {{
                        ref var key = ref valueList.ValueByRef(i);

                        if (validCheck(key.Ref))
                        {{
                            return key.Value;
                        }}
                    }}

                    return default({typeName});                  
                }}

                public void Union(IIndexStorage dirtyIndex)
                {{
                    var index = ({typeName}Index)dirtyIndex;

                    if (index.m_removeItems is not null && Count > 0)
                    {{
                        foreach (var item in index.m_removeItems)
                        {{
                            Remove(item.Value, item.Ref);
                        }}
                    }}

                    if (index.m_removeItems is not null && Count > 0)
                    {{
                        foreach (var item in index.m_removeItems)
                        {{
                            Remove(item.Value, item.Ref);
                        }}
                     }}

                    if (m_notNullValueList.Count == 0)
                    {{
                        index.m_notNullValueList.Sort(CompareIndexValues);

                        m_notNullValueList.AddRange(index.m_notNullValueList);
                        m_nullReferencesList.AddRange(index.m_nullReferencesList);    
                        
                        return;
                    }}
                   
                    foreach (var dirtyKey in index.m_notNullValueList)
                    {{
                        Add(dirtyKey.Value, dirtyKey.Ref);
                    }}
                    
                    m_nullReferencesList.AddRange(index.m_nullReferencesList); 
                }}

                public int Count
                {{
                    get
                    {{
                        return m_notNullValueList.Count + m_nullReferencesList.Count;
                    }}
                }}  

                public void AddNotNull({typeName} value, int reference)
                {{                   
                    int num = BinarySearch(m_notNullValueList, ref value);                

                    if (num >= 0)
                    {{
                        if (num >= m_notNullValueList.Count - 1)
                        {{                
                           m_notNullValueList.Add(new {typeName}IndexKey() {{ Value = value, Ref = reference }});
				        }}
                        else
                        {{	
                            m_notNullValueList.Insert(num, new {typeName}IndexKey() {{ Value = value, Ref = reference }});
                        }}
                    }}
                    else
                    {{	 
                         m_notNullValueList.Insert(~num, new {typeName}IndexKey() {{ Value = value, Ref = reference}});
                    }}
                }}

                internal int SearchNotNull({typeName} value)
                {{
                    var startIndex = 0;
                    var endIndex = m_notNullValueList.Count - 1;

                    return BinarySearch(m_notNullValueList, ref value, ref startIndex, ref endIndex);
                }}              

                public int SearchNull()
	            {{
                    if (m_nullReferencesList.Count > 0)
                    {{
                        return m_nullReferencesList[0];
                    }}

                    return -1;
                }}

                public IEnumerable<int> SearchNullRange()
	            {{
                     return m_nullReferencesList;
                }} 

                private int SearchCore({typeName} value)
                {{
                      var indx = BinarySearch(m_notNullValueList, ref value);

                      if (indx >= 0)
                      {{
                           return m_notNullValueList.ValueByRef(indx).Ref;
                      }}

                      return -1;
                 }}

                private IEnumerable<int> SearchRangeCore({typeName} value)
                {{
                      var startIndex = BinarySearchLeft(m_notNullValueList, ref value);                     

                      var end = m_notNullValueList.Count - 1;

                      if (startIndex >= 0)
                      {{
                           var rightIndex = BinarySearchRight(m_notNullValueList, ref value, ref startIndex, ref end);

                           if (rightIndex >= 0 && rightIndex < m_notNullValueList.Count)
                           {{
                                for(int i = startIndex; i <= rightIndex; i++)
                                {{
                                     yield return  m_notNullValueList.ValueByRef(i).Ref;
                                }}
                           }}
                           else
                           {{
                                yield return m_notNullValueList.ValueByRef(startIndex).Ref;
                           }}

                       }}
                 }}

                private int RemoveRangeCore({typeName} value, int reference)
                {{
                      var startIndex = BinarySearchLeft(m_notNullValueList, ref value);                     

                      var end = m_notNullValueList.Count - 1;

                      int count = 0;

                      if (startIndex >= 0)
                      {{
                           var rightIndex = BinarySearchRight(m_notNullValueList, ref value, ref startIndex, ref end);

                           if (rightIndex >= 0 && rightIndex < m_notNullValueList.Count)
                           {{
                                for(int i = rightIndex; i >= startIndex; i--)
                                {{
                                    if(m_notNullValueList[i].Ref == reference)
                                    {{
                                        m_notNullValueList.RemoveAt(i);

                                        count++;
                                    }}
                                }}
                           }}
                           else
                           {{
                                if(m_notNullValueList[startIndex].Ref == reference)
                                {{
                                    m_notNullValueList.RemoveAt(startIndex);

                                    count++;
                                }}
                           }}
                       }}

                    return count;
                 }}

                 internal int BinarySearch(Data<{typeName}IndexKey> list, ref {typeName} predicate)
                 {{
                      var startIndex = 0;
                      var endIndex = list.Count - 1;

                     return BinarySearch(list, ref predicate, ref startIndex, ref endIndex);
                 }}

                internal int BinarySearchLeft(Data<{typeName}IndexKey> list, ref {typeName} predicate)
                {{
                      var startIndex = 0;
                      var endIndex = list.Count - 1;

                     return BinarySearchLeft(list, ref predicate, ref startIndex, ref endIndex);
                }}

                internal int BinarySearchRight(Data<{typeName}IndexKey> list, ref {typeName} predicate)
                {{
                      var startIndex = 0;
                      var endIndex = list.Count - 1;

                     return BinarySearchRight(list, ref predicate, ref startIndex, ref endIndex);
                }}

                public void Clear()
                {{
                    m_notNullValueList.Clear();
                    m_nullReferencesList.Clear();
                }}		      
                
                IEnumerable<(IComparable key, bool hasValue, int reference)> IIndexComparableStorageTyped<IComparable>.GetKeyValues()
                {{
                    foreach (var nullReference in m_nullReferencesList)
                    {{
                        yield return (default({typeName}), false, nullReference);
                    }}

                    foreach (var kv in m_notNullValueList)
                    {{
                        yield return (kv.Value, true, kv.Ref);
                    }}
                }}

                public IEnumerable<(IComparable key, bool hasValue, int reference)> GetComparableKeyValues()
                {{
                    foreach (var nullReference in m_nullReferencesList)
                    {{
                        yield return (default({typeName}), false, nullReference);
                    }}

                    foreach (var kv in m_notNullValueList)
                    {{
                        yield return (kv.Value, true, kv.Ref);
                    }}
                }}

                public IEnumerable<({typeName} key, bool hasValue, int reference)> GetKeyValues()
                {{
                    foreach (var nullReference in m_nullReferencesList)
                    {{
                        yield return (default({typeName}), false, nullReference);
                    }}

                    foreach (var kv in m_notNullValueList)
                    {{
                        yield return (kv.Value, true, kv.Ref);
                    }}
                }}

                 public IReadOnlyList<int> CheckAllKeys(IIndexStorage storage, Func<int, bool> validCheck,  Func<int, bool> storageValidCheck)
                 {{
                    var errorReferences = new Data<int>();

                    var typedIndex = (IIndexComparableStorageTyped<{typeName}>)storage;

                    foreach (var kv in typedIndex.GetKeyValues())
                    {{
                         if(kv.hasValue == false) 
                         {{ 
                               continue;
                         }}

                         if(storageValidCheck(kv.reference) == false) 
                         {{ 
                                continue;
                         }}

                         var range = this.SearchRangeCore(kv.key);  
                       
                         bool missingInIndex = true;                      

                         foreach(var reference in range)
                         {{
                            if (validCheck(reference))
                            {{
                                 missingInIndex = false;
                                 break;
                             }}
                         }}

                         if (missingInIndex)
                         {{
                              errorReferences.Add(kv.reference);
                         }}
                    }}   

                    return errorReferences;
                }} 

                {compareIndexesCode}

                {binarySearchCode}

                {comparableCode}	

                {typedNullableCode}

                {typedCode}	
        }}
}}
");
                
                context.AddSource($"Create{typeName}IndexGenerator", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
            }
        }

        private static string GetComparableCode(string typeName, string argType, bool canBeNull)
        {
            var comparableCode = $@"

                public void AddRemovedItem({argType} oldValue, int reference)
                {{
                        if (m_removeItems == null)
                        {{
                            m_removeItems = new();
                        }}
                    
                        m_removeItems.Add(new {typeName}IndexKey() {{Value = ({typeName})oldValue, Ref = reference }});
                }}

                public bool Update({argType} value, int reference, {argType} oldValue)
                {{
                     var isNull = { (canBeNull ? "value is null" : "false") };
                     var isOldNull = { (canBeNull ? "oldValue is null" : "false") }; 

                     bool updated = false;

                     if(isOldNull)
                     {{
                         updated = m_nullReferencesList.RemoveAll(reference) > 0;
                     }}
                     else 
                     {{                       
                         updated = RemoveRangeCore(({typeName})oldValue, reference) > 0;
                     }} 
                 
                    if (updated)
                    {{
                        if (isNull)
                        {{
                            m_nullReferencesList.Add(reference);
                        }}
                        else
                        {{
                            Add(value, reference);
                        }}
                    }}

                    return updated;
                }}

                public bool Remove({argType} key, int reference)
                {{         
                    var keyIsNull = { (canBeNull ? "key is null" : "false") };

                    if (keyIsNull)
                    {{
                         return m_nullReferencesList.RemoveAll(reference) > 0;
                    }}           
                                  
                    return RemoveRangeCore(({typeName})key, reference) > 0;
                }}

                public void Add({argType} value, int reference)
                {{
                    var valueIsNull = { (canBeNull ? "value is null" : "false") };

                    if (valueIsNull)
                    {{
                        m_nullReferencesList.Add(reference);
                    }}
                    else
                    {{
                        AddNotNull(({typeName})value, reference);
                    }}
                }}

                public int Search({argType} value)
                {{
                    var valueIsNull = { (canBeNull ? "value is null" : "false") };

                    if (valueIsNull)
                    {{               
                        return SearchNull();
                    }}

			        return SearchCore(({typeName})value);
                }}

                public IEnumerable<int> SearchRange({argType} value)
                {{
                    var valueIsNull = { (canBeNull ? "value is null" : "false") };

                    if (valueIsNull)
                    {{               
                        return SearchNullRange();
                    }}

			        return SearchRangeCore(({typeName})value);
                }}

                public IEnumerable<int> SearchAll({argType} value)
                {{
                    var valueIsNull = { (canBeNull ? "value is null" : "false") };

                    if (valueIsNull)
                    {{
                        int ind = m_notNullValueList.Count;

                        for (var index = 0; index < m_nullReferencesList.Count; index++)
                        {{
                            var reference = m_nullReferencesList[index];

                            yield return reference;
                        }}
                    
                        yield break;
                    }}
                
                    var range = SearchRange(value);                 

                    foreach (int i in range)
                    {{
                        yield return i;
                    }}
                }}
";
            return comparableCode;
        }
    }
}