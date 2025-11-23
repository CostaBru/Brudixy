using System;
using System.Collections.Generic;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public class CoreContainerDataProps
    {
        [CanBeNull] public Data<object> OriginalData;
        public ulong Age;
        public ulong AnnotationAge;
        public Map<string, ExtPropertyValue> ExtProperties;
        public RowState DataRowState;
        public Data<object> Data;
        public Set<string> ChangedFields;
        public int RowHandle;
        public int? DisplayDateTimeUtcOffsetTicks { get; set; }
        
        public CoreContainerDataProps(int rowHandle, 
            Data<object> data,
            RowState dataRowState = RowState.Unchanged,
            int? displayDateTimeUtcOffsetTicks = null,
            Map<string, ExtPropertyValue> extProperties = null,
            Data<object> originalData = null,
            ulong age = 1,
            ulong annAge = 1,
            Set<string> changedFields = null)
        {
            RowHandle = rowHandle;
            Data = data;
            DataRowState = dataRowState;
            ExtProperties = extProperties;
            OriginalData = originalData;
            AnnotationAge = annAge;
            ChangedFields = changedFields ?? new Set<string>();
            Age = age;
            DisplayDateTimeUtcOffsetTicks = displayDateTimeUtcOffsetTicks;
        }
        
        public CoreContainerDataProps([NotNull] ICoreDataRowReadOnlyAccessor row, IReadOnlyCollection<string> skipColumns = null)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            var count = row.GetColumnCount();

            var data = new Data<object>();

            var keepOriginalData = row.RowRecordState == RowState.Modified;
            
            data.Ensure(count);

            Data<object> originalData = null;
            
            if (keepOriginalData)
            {
                originalData = new Data<object>();
                
                originalData.Ensure(count);
            }

            int index = 0;
            
            foreach (var column in row.GetColumns())
            {
                if (skipColumns.ListNullOrItemAbsent(column.ColumnName))
                {
                    data[index] = row[column];

                    if (keepOriginalData)
                    {
                        originalData[index] = row.GetOriginalValue(column);
                    }
                }

                index++;
            }

            Data = data;
            OriginalData = originalData;

            foreach (var extendedProperty in row.GetXProperties())
            {
                if (ExtProperties == null)
                {
                    ExtProperties = new Map<string, ExtPropertyValue>(StringComparer.OrdinalIgnoreCase);
                }

                var xValue = row.GetXProperty<object>(extendedProperty);
                var originalXValue = keepOriginalData ? row.GetXProperty<object>(extendedProperty, true) : xValue;

                ExtProperties[extendedProperty] = new ExtPropertyValue
                {
                    Current = xValue == originalXValue ? null : xValue,
                    Original = originalXValue
                };
            }

            foreach (var changedField in row.GetChangedFields())
            {
                ChangedFields = ChangedFields ??= new Set<string>(StringComparer.OrdinalIgnoreCase);

                ChangedFields.Add(changedField);
            }

            if (row is CoreDataRow cr)
            {
                DisplayDateTimeUtcOffsetTicks = cr.table?.DisplayDateTimeUtcOffsetTicks;
            }
            else if (row is CoreDataRowContainer crc)
            {
                DisplayDateTimeUtcOffsetTicks = crc.ContainerDataProps.DisplayDateTimeUtcOffsetTicks;
            }

            DataRowState = row.RowRecordState;
            RowHandle = row.RowHandle;
            Age = row.GetRowAge();
        }

        public virtual void Dispose()
        {
            DataRowState = RowState.Disposed;

            Data?.Dispose();
            ExtProperties?.Dispose();
            ChangedFields?.Dispose();
        }

        public CoreContainerDataProps Clone()
        {
            return CloneCore();
        }

        protected virtual CoreContainerDataProps CloneCore()
        {
            var newObj = (CoreContainerDataProps) MemberwiseClone();

            if (ExtProperties != null)
            {
                newObj.ExtProperties = new Map<string, ExtPropertyValue>(ExtProperties, StringComparer.OrdinalIgnoreCase);
            }

            if (ChangedFields != null)
            {
                newObj.ChangedFields = new Set<string>(ChangedFields, StringComparer.OrdinalIgnoreCase);
            }

            newObj.Data = CopyData(Data);
            
            if (OriginalData != null)
            {
                newObj.OriginalData = CopyData(OriginalData);
            }
            
            newObj.Age = Age;

            newObj.DataRowState = DataRowState;

            return newObj;
        }

        private static Data<object> CopyData(Data<object> data)
        {
            var newData = new Data<object>(data.Count);
            newData.Ensure(data.Count);

            for (int i = 0; i < data.Count; i++)
            {
                var value = data[i];
                
                CoreDataRowContainer.CopyIfNeededBoxed(ref value);
                
                newData[i] = value;
            }

            return newData;
        }
    }
}