using System;
using System.Linq;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        public IComparable Max(string column, Tuple<string, IComparable> filter = null) => CalcColumnMinMax(column, calcMax: true, filter: filter);

        public IComparable GetAnyMax(string column, Tuple<string, IComparable> filter = null) => CalcColumnMinMax(column, calcMax: true, filter: filter, canUseAutoInc: true);

        public IComparable Min(string column, Tuple<string, IComparable> filter = null) => CalcColumnMinMax(column, calcMax: false, filter: filter);

        public IComparable Max(ICoreTableReadOnlyColumn column, Tuple<string, IComparable> filter = null)
        {
            if (column is CoreDataColumn dataColumn)
            {
                return CalcColumnMinMax(dataColumn, calcMax: true, filter: filter);
            }

            return CalcColumnMinMax(column.ColumnName, calcMax: true, filter: filter);
        }

        public IComparable Min(ICoreTableReadOnlyColumn column, Tuple<string, IComparable> filter = null)
        {
            if (column is CoreDataColumn dataColumn)
            {
                return CalcColumnMinMax(dataColumn, calcMax: false, filter: filter);
            }

            return CalcColumnMinMax(column.ColumnName, calcMax: false, filter: filter);
        }

        private IComparable CalcColumnMinMax(string column, bool calcMax, Tuple<string, IComparable> filter = null,  bool canUseAutoInc = false)
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(column, out var dataColumn))
            {
                return CalcMinMaxInternal(calcMax, dataColumn, filter, canUseAutoInc);
            }

            return null;
        }

        private IComparable CalcColumnMinMax(CoreDataColumn column, bool calcMax, Tuple<string, IComparable> filter = null, bool canUseAutoInc = false)
        {
            return CalcMinMaxInternal(calcMax, column, filter, canUseAutoInc);
        }

        private IComparable CalcMinMaxInternal(bool calcMax, CoreDataColumn column, [CanBeNull] Tuple<string, IComparable> filter = null, bool canUseAutoInc = false)
        {
            if (filter != null)
            {
                var filterColumnHandle = GetColumn(filter.Item1).ColumnHandle;
                var filterValue = filter.Item2;

                var handles = FindManyHandles(filterColumnHandle, filterValue);

                var dataItem = column.DataStorageLink;

                return dataItem.CalcMinMax(calcMax, column, handles);
            }
            else
            {
                var dataItem = column.DataStorageLink;

                if (canUseAutoInc && column.IsAutomaticValue)
                {
                    return (IComparable)dataItem.GetCurrentMax(column);
                }
                
                return dataItem.CalcMinMax(calcMax, column, StateInfo.RowHandles);
            }
        }
    }
}