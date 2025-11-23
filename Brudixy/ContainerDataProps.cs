using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public class ContainerDataProps : CoreContainerDataProps
    {
        public Map<string, object> RowAnnotations;
        public Map<string, Map<string, object>> CellAnnotations;
        public Map<string, Map<string, object>> XPropInfo;
        
        public ContainerDataProps(int rowHandle,
            Data<object> data,
            RowState dataRowState = RowState.Unchanged,
            int? displayDateTimeUtcOffsetTicks = null,
            Map<string, ExtPropertyValue> extProperties = null,
            Data<object> originalData = null,
            ulong age = 0,
            IEnumerable<(string type, object value)> rowAnnotations = null,
            Map<string, Map<string, object>> cellAnnotations = null,
            Map<string, Map<string, object>> xPropInfo = null) : 
            base(rowHandle, data, dataRowState: dataRowState, displayDateTimeUtcOffsetTicks: displayDateTimeUtcOffsetTicks, extProperties: extProperties, originalData: originalData, age: age)
        {
            if (rowAnnotations != null)
            {
                RowAnnotations = new();

                foreach (var ann in rowAnnotations)
                {
                    RowAnnotations[ann.type] = ann.value;
                }
            }
            
            CellAnnotations = cellAnnotations;
            XPropInfo = xPropInfo;
        }
        
        public ContainerDataProps([NotNull] ICoreDataRowReadOnlyAccessor row, IReadOnlyCollection<string> skipColumns = null)
        : base(row, skipColumns)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }
           
            if (row is IDataRowReadOnlyAccessor dr)
            {
                var rowAnnotations = dr.GetRowAnnotations();
                
                RowAnnotations = new Map<string, object>();

                foreach (var annotation in rowAnnotations)
                {
                    RowAnnotations[annotation.type] = annotation.value;
                }

                foreach (var infoColumn in dr.GetCellAnnotations())
                {
                    if (CellAnnotations == null)
                    {
                        CellAnnotations = new();
                    }

                    if (CellAnnotations.TryGetValue(infoColumn.column, out var columnInfo) == false)
                    {
                        CellAnnotations[infoColumn.column] = columnInfo = new();
                    }
                    
                    columnInfo[infoColumn.type] = infoColumn.value;
                }
            }
        }

        public override void Dispose()
        {
           base.Dispose();

            if (CellAnnotations != null)
            {
                foreach (var vals in CellAnnotations.Values)
                {
                    vals.Dispose();
                }
                
                CellAnnotations.Dispose();
            }
            
            XPropInfo?.Dispose();
        }

        public new ContainerDataProps Clone() => (ContainerDataProps)base.Clone();

        protected override CoreContainerDataProps CloneCore()
        {
            var newObj = (ContainerDataProps) base.CloneCore();
        
            if (CellAnnotations != null)
            {
                newObj.CellAnnotations = new();

                foreach (var kv in CellAnnotations)
                {
                    newObj.CellAnnotations[kv.Key] = new Map<string, object>(kv.Value);
                }
            }
            
            if (XPropInfo != null)
            {
                newObj.XPropInfo = new Map<string, Map<string, object>>();

                foreach (var xp in XPropInfo)
                {
                    if (xp.Value != null)
                    {
                        newObj.XPropInfo[xp.Key] = new Map<string, object>(xp.Value);
                    }
                }
            }

            if (RowAnnotations != null)
            {
                newObj.RowAnnotations = new (RowAnnotations);
            }

            return newObj;
        }

        public void SetXPropertyInfo(string propertyCode, string key, object value)
        {
            if (XPropInfo == null)
            {
                XPropInfo = new Map<string, Map<string, object>>(StringComparer.OrdinalIgnoreCase);
            }

            var xPropStorage = XPropInfo.GetOrAdd(propertyCode, () => new Map<string, object>());
            
            xPropStorage[key] = value;
        }

        public object GetXPropertyInfo(string propertyCode, string key)
        {
            return XPropInfo?.GetOrDefault(propertyCode)?.GetOrDefault(key);
        }

        public IReadOnlyDictionary<string,object> GetXPropertyInfoValues(string propertyCode)
        {
            return XPropInfo?.GetOrDefault(propertyCode);
        }
    }
}