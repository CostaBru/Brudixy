using Brudixy.Converter;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    public class RowCellAnnotation
    {
        internal ValueInfo ValueInfo;
        internal DataTable DataTable;

        public RowCellAnnotation([NotNull] DataTable table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }
            
            ValueInfo = new ();

            DataTable = table;
        }

        public void ClearAnnotations()
        {
            ValueInfo.Clear();
        }
        
        public string GetCellInfo(int rowHandle) => GetCellAnnotations<string>(rowHandle, ValueInfo.Info) as string ?? string.Empty;

        public string GetCellError(int rowHandle) => GetCellAnnotations<string>(rowHandle, ValueInfo.Error) as string ?? string.Empty;

        public string GetCellWarning(int rowHandle) => GetCellAnnotations<string>(rowHandle, ValueInfo.Warning) as string ?? string.Empty;

        public T GetCellAnnotations<T>(int rowHandle, string type)
        {
            if (ValueInfo == null || rowHandle < 0 || (DataTable.StateInfo.GetRowState(rowHandle) == RowState.Detached))
            {
                return TypeConvertor.ReturnDefault<T>();
            }

            return ValueInfo.GetRowAnnotation<T>(rowHandle, type);
        }
        
        public IEnumerable<(string type, string value)> GetCellAnnotations(int rowHandle)
        {
            if (ValueInfo == null || rowHandle < 0 || (DataTable.StateInfo.GetRowState(rowHandle) == RowState.Detached))
            {
                yield break;
            }

            foreach (var kv in ValueInfo.RowAnnotations)
            {
                var data = (string)kv.Value.GetData(rowHandle);
                
                if (string.IsNullOrEmpty(data) == false)
                {
                    yield return (kv.Key, data);
                }
            }
        }
        
        public bool HasCellAnnotation(int rowHandle, string type)
        {
            if (ValueInfo == null || rowHandle < 0 || (DataTable.StateInfo.GetRowState(rowHandle) == RowState.Detached))
            {
                return false;
            }

            return ValueInfo.HasRowAnnotation(rowHandle, type);
        }

        public bool HasCellError(int rowHandle) => HasCellAnnotation(rowHandle, ValueInfo.Error);

        public bool HasCellWarning(int rowHandle) => HasCellAnnotation(rowHandle, ValueInfo.Warning);

        public bool HasCellInfo(int rowHandle) => HasCellAnnotation(rowHandle, ValueInfo.Info);

        public void SetCellInfo(int rowHandle, int columnHandle, string info, int? tranId)
        {
            SetCellStringAnnotation(rowHandle, info, tranId, ValueInfo.Info);
        }
        
        public void SetCellWarning(int rowHandle, int columnHandle, string info, int? tranId)
        {
            SetCellStringAnnotation(rowHandle, info, tranId, ValueInfo.Warning);
        }
        
        public void SetCellError(int rowHandle, int columnHandle, string error, int? tranId)
        {
            SetCellStringAnnotation(rowHandle, error, tranId, ValueInfo.Error);
        }
        
        public bool SetCellStringAnnotation(int rowHandle, string value, int? tranId, string type)
        {
            if (rowHandle < 0 || (DataTable.StateInfo.GetRowState(rowHandle) == RowState.Detached))
            {
                return false;
            }

            return ValueInfo.SetRowAnnotation(DataTable, rowHandle, string.IsNullOrEmpty(value) ? null : value, tranId, type);
        }

        public bool SetCellAnnotation(int rowHandle, object value, int? tranId, string type)
        {
            if (rowHandle < 0 || (DataTable.StateInfo.GetRowState(rowHandle) == RowState.Detached))
            {
                return false;
            }

            return ValueInfo.SetRowAnnotation(DataTable, rowHandle, value, tranId, type);
        }

        public RowCellAnnotation Copy(CoreDataTable table)
        {
            var clone = (RowCellAnnotation)this.MemberwiseClone();

            clone.ValueInfo = ValueInfo?.Clone(table);

            return clone;
        }
        
        public RowCellAnnotation Clone()
        {
            var clone = (RowCellAnnotation)this.MemberwiseClone();

            clone.ValueInfo = new ValueInfo();

            return clone;
        }

        public bool RollbackRowTransaction(int rowHandle, int tranId)
        {
            ValueInfo.RollbackRowTransaction(rowHandle, tranId);
            
            return true;
        }

        public void StopLoggingTransactionChanges(int rowHandle)
        {
            ValueInfo.StopLoggingTransactionChanges(rowHandle);
        }

        public void Dispose()
        {
            ValueInfo.Dispose();

            DataTable = null;
        }
    }
}