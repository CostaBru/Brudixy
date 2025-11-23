using System;
using System.Collections.Generic;

namespace Brudixy.Storage
{
    partial class StringStorage : ICompareStorage, ICompareStorageTyped<string>
    {
        public int Compare(string val1, string val2)
        {
            return String.Compare(val1, val2, StringComparison.Ordinal);
        }	
        
        public IEnumerable<int> Filter<V>(V value) where V : IComparable
        {
            var notNullValue = default(string);

            if(value is System.String nv) 
            { 
                notNullValue = nv; 
            }
            else 
            { 
                notNullValue = (string)Convert.ChangeType(value, typeof(System.String));
            }

            //common case
            var root = this.GetRoot();

            if (root?.Storage != null)
            {
                var cnt = root.Size;
                var items = root.Storage;

                for(int i = 0; i < cnt && i < items.Length; i++)
                {
                    if (Compare(items[i], notNullValue) == 0)
                    {
                        yield return i;
                    }
                }
            }
            else 
            {
                var cnt = Count;

                for(int i = 0; i < cnt; i++)
                {
                    if (Compare(this.ValueByRef(i), notNullValue) == 0)
                    {
                        yield return i;
                    }
                }
            }
        }
    }
}
