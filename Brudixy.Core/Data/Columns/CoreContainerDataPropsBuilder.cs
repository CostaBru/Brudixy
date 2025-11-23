using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public class CoreContainerDataPropsBuilder
    {
        public CoreContainerDataPropsBuilder()
        {
        }
        
        [CanBeNull] public Data<object> OriginalData;
        public ulong Age = 1;
        public ulong AnnotationAge = 1;
        public Map<string, ExtPropertyValue> ExtProperties;
        public RowState DataRowState;
        public Data<object> Data;
        public Set<string> ChangedFields;
        public int RowHandle;
        public int? DisplayDateTimeUtcOffsetTicks { get; set; }

        public virtual CoreContainerDataProps ToProps()
        {
            return new CoreContainerDataProps(RowHandle,
                Data,
                DataRowState, 
                DisplayDateTimeUtcOffsetTicks,
                ExtProperties,
                OriginalData,
                Age, 
                AnnotationAge,
                ChangedFields) ;
        }
    }
}