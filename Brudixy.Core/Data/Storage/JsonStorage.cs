using System.Text.Json.Nodes;
using Brudixy.Converter;
using Konsarpoo.Collections;

namespace Brudixy.Storage
{
    internal partial class JsonStorage
    {
        public  JsonStorage(int capacity) : base(capacity)
        {         
        }

        private  JsonStorage(JsonStorage data) : base(data)
        {          
        }


        private  JsonStorage(Data<JsonObject> data) : base(data)
        {            
        }	
        
        public object Clone()
        {
            var objects = new Data<JsonObject>(Count);
            
            objects.Ensure(Count);

            for (int i = 0; i < Count; i++)
            {
                var value = this[i];

                if (value != null)
                {
                    objects[i] = (JsonObject)value.DeepClone();
                }
            }

            return new JsonStorage(objects);
        }
    }
}