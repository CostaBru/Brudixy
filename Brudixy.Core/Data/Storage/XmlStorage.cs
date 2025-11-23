using System.Xml.Linq;
using Konsarpoo.Collections;

namespace Brudixy.Storage
{
    internal partial class XmlStorage
    {
        public  XmlStorage(int capacity) : base(capacity)
        {         
        }

        private  XmlStorage(XmlStorage data) : base(data)
        {          
        }


        private  XmlStorage(Data<XElement> data) : base(data)
        {            
        }		
        
        public object Clone()
        {
            var objects = new Data<XElement>(Count);
            
            objects.Ensure(Count);

            for (int i = 0; i < Count; i++)
            {
                var value = this[i];

                if (value != null)
                {
                    objects[i] = new XElement(value);
                }
            }

            return new XmlStorage(objects);
        }
    }
}