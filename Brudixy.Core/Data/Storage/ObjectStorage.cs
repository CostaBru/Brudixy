using System;
using Konsarpoo.Collections;

namespace Brudixy.Storage
{
    public partial class ObjectStorage
    {
        public  ObjectStorage(int capacity) : base(capacity)
        {         
        }
        
        public ObjectStorage(TypeCode typeCode, int capacity):
            base(capacity)
        {
            TypeCode = typeCode;
        }

        private ObjectStorage(TypeCode typeCode, Data<object> data) : base (data)
        {
            TypeCode = typeCode;
        }

        public TypeCode TypeCode;

        public object Clone()
        {
            var objects = new Data<object>(Count);

            for (int i = 0; i < Count; i++)
            {
                if (this[i] is ICloneable cloneable)
                {
                    objects.Add(cloneable.Clone());
                }
                else
                {
                    objects.Add(this[i]);
                }
            }

            return new  ObjectStorage(TypeCode, objects);
        }
    }
}