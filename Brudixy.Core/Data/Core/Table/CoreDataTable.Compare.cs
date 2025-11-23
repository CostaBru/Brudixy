using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        public int CompareTo(object obj)
        {
            if (obj is CoreDataTable td)
            {
                return CompareTo(td);
            }

            return -1;
        }

        public int CompareTo(CoreDataTable other)
        {
            var tuple = CompareToExt(other);
            
            return tuple.cmp;
        }

        public (int cmp, string name, string type) CompareToExt(CoreDataTable other)
        {
            if (ReferenceEquals(this, other))
            {
                return (0, "Same reference", string.Empty);
            }

            if (other is null)
            {
                return (-1, "Other is null", string.Empty);
            }

            var compareTo = CompareMetadata(other);
            
            if (compareTo.cmp != 0)
            {
                return compareTo;
            }

            var rowCntCompareTo = RowCount.CompareTo(other.RowCount);

            if (rowCntCompareTo != 0)
            {
                return (rowCntCompareTo, "RowCount is different", string.Empty);
            }

            GetOrderedRowsByPk(other, out var e1, out var e2, out compareTo);
            
            if (compareTo.cmp != 0)
            {
                return compareTo;
            }

            while (e1.MoveNext() && e2.MoveNext())
            {
                if (e1.Current.Equals(e2.Current) == false)
                {
                    compareTo = e1.Current.CompareToExt(e2.Current);

                    if (compareTo.cmp != 0)
                    {
                        return compareTo;
                    }
                }
            }

            compareTo = CompareXProperties(this.ExtProperties, other.ExtProperties);

            return compareTo;
        }

        private void GetOrderedRowsByPk(CoreDataTable other, out IEnumerator<CoreDataRow> e1, out IEnumerator<CoreDataRow> e2, out (int, string, string) compareTo)
        {
            var otherkey = other.PrimaryKey.ToArray();

            e1 = null;
            e2 = null;

            if (DataColumnInfo.PrimaryKeyColumns.Length == otherkey.Length && DataColumnInfo.PrimaryKeyColumns.Length > 0)
            {
                if (DataColumnInfo.PrimaryKeyColumns.Length == 1 && otherkey.Length == 1)
                {
                    var pkCol1 = DataColumnInfo.PrimaryKeyColumns.First().ColumnName;
                    var pkCol2 = otherkey[0].ColumnName;

                    var cmp = string.Compare(pkCol1, pkCol2, StringComparison.Ordinal);

                    if (cmp != 0)
                    {
                        compareTo = (cmp, pkCol1, "PK column name");
                        
                        return;
                    }

                    e1 = Rows.OrderBy(pkCol1).GetEnumerator();
                    e2 = other.Rows.OrderBy(pkCol2).GetEnumerator();
                }
                else
                {
                    var pkCols1 = DataColumnInfo.PrimaryKeyColumns
                        .Select(c => GetDataColumnInstance(c.ColumnHandle))
                        .OfType<ICoreTableReadOnlyColumn>()
                        .ToData();
                    
                    var pkCols2 = otherkey.OfType<ICoreTableReadOnlyColumn>().ToData();

                    e1 = Rows.OrderBy(pkCols1).GetEnumerator();
                    e2 = other.Rows.OrderBy(pkCols2).GetEnumerator();
                }
            }
            else
            {
                e1 = Rows.GetEnumerator();
                e2 = other.Rows.GetEnumerator();
            }

            compareTo = (0, string.Empty, string.Empty);
        }

        private (int cmp, string name, string type) CompareMetadata(CoreDataTable other)
        {
            var compareTo = string.Compare(Name, other.Name, StringComparison.Ordinal);

            if (compareTo != 0)
            {
                return (compareTo, "TableName", "Meta data: table Name");
            }

            compareTo = ColumnCount.CompareTo(other.ColumnCount);

            if (compareTo != 0)
            {
                return (compareTo, "Column count", "Meta data: Column count");
            }

            var columnsCount = DataColumnInfo.ColumnsCount;

            for (var index = 0; index < columnsCount; index++)
            {
                var column1 = DataColumnInfo.Columns[index].ColumnName;
                var column2 = other.DataColumnInfo.Columns[index].ColumnName;

                compareTo = string.Compare(column1, column2, StringComparison.Ordinal);

                if (compareTo != 0)
                {
                    return (compareTo, column1, "Meta data: Column name");
                }
            }

            return (0, string.Empty, string.Empty);
        }

        public bool Equals(CoreDataTable other)
        {
            var tuple = EqualsExt(other);
            
            return tuple.value;
        }

        public (bool value, string name, string type) EqualsExt(CoreDataTable other)
        {
            if (ReferenceEquals(this, other))
            {
                return (true, string.Empty, "ReferenceEquals");
            }

            if (other is null)
            {
                return (false, string.Empty, "other is null");
            }

            var compareTo = CompareMetadata(other);
            
            if (compareTo.cmp != 0)
            {
                return (false, compareTo.name, compareTo.type);
            }

            var rCntCompareTo = RowCount.CompareTo(other.RowCount);

            if (rCntCompareTo != 0)
            {
                return (false, "RowCount", "RowCount");
            }

            GetOrderedRowsByPk(other, out var e1, out var e2, out compareTo);
            
            if (compareTo.cmp != 0)
            {
                return (false, compareTo.name, compareTo.type);
            }

            while (e1.MoveNext() && e2.MoveNext())
            {
                var equalsCore = e1.Current.EqualsExt(e2.Current);
                
                if (equalsCore.value == false)
                {
                    return equalsCore;
                }
            }

            var compareXProperties = CompareXProperties(this.ExtProperties, other.ExtProperties);
            
            if (compareXProperties.cmp != 0)
            {
                return (false, compareXProperties.name, compareXProperties.type);
            }

            return (true, string.Empty, string.Empty);
        }

        private static (int cmp, string name, string type) CompareXProperties(Map<string, ExtPropertyValue> thisXProps, Map<string, ExtPropertyValue> otherXProps)
        {
            if (thisXProps is null != otherXProps is null)
            {
                if (otherXProps is null)
                {
                    return (-1, "Other XProp is null", "table XProp");
                }

                return (1, "This XProp is null", "table XProp");
            }

            if (thisXProps == null)
            {
                return (0, "Both XProp are null", "table XProp");
            }
            
            int compareTo;
            var xc1 = thisXProps?.Count ?? 0;
            var xc2 = otherXProps?.Count ?? 0;

            compareTo = xc1.CompareTo(xc2);

            if (compareTo != 0)
            {
                return (compareTo, "XProp count", "table XProp");
            }

            var x1 = thisXProps.Keys.OrderBy(x => x).GetEnumerator();
            var x2 = otherXProps.Keys.OrderBy(x => x).GetEnumerator();

            while (x1.MoveNext() && x2.MoveNext())
            {
                var x1Property = x1.Current;
                var x2Property = x2.Current;

                compareTo = string.Compare(x1Property, x2Property, StringComparison.OrdinalIgnoreCase);

                if (compareTo != 0)
                {
                    return (compareTo, x1Property , "table XProp: XProp nam");
                }

                var xv1 = thisXProps.GetOrDefault(x1Property);
                var xv2 = otherXProps.GetOrDefault(x1Property);

                var xvVal = xv1.Current ?? xv1.Original;
                var rowVal = xv2.Current ?? xv2.Original;

                var thisNull = xv1.Current is null || (xv1.Current is string s1 && string.IsNullOrEmpty(s1));
                var otherNull = xv2.Current is null || (xv2.Current is string s2 && string.IsNullOrEmpty(s2));

                if (thisNull)
                {
                    thisNull = xv1.Original is null || (xv1.Original is string ss1 && string.IsNullOrEmpty(ss1));
                }
                
                if (otherNull)
                {
                    otherNull = xv2.Original is null || (xv2.Original is string ss2 && string.IsNullOrEmpty(ss2));
                }
                
                if (thisNull != otherNull)
                {
                    if (otherNull)
                    {
                        return (-1, x1Property, "table XProp: Other XProp value is null");
                    }

                    return (1, x1Property, "table XProp: This XProp value is null");
                }

                if (thisNull == false)
                {
                    if (xvVal is IComparable c1 && rowVal is IComparable c2)
                    {
                        var cmp = c1.CompareTo(c2);

                        if (cmp != 0)
                        {
                            return (cmp, x1Property, "table XProp");
                        }
                    }
                    else
                    {
                        var deepEquals = CoreDataRowContainer.DeepEquals(xvVal, rowVal);


                        if (deepEquals == false)
                        {
                            return (-1, x1Property, "table XProp");
                        }
                    }
                }
            }

            return (0, string.Empty, string.Empty);
        }

        internal static int CompareStrings(string s1, string s2)
        {
            object obj1 = s1;
            object obj2 = s2;

            if (obj1 == obj2)
            {
                return 0;
            }

            if (obj1 == null)
            {
                return -1;
            }

            if (obj2 == null)
            {
                return 1;
            }

            int length1 = s1.Length;
            int length2 = s2.Length;

            while (length1 > 0 && (s1[length1 - 1] == 32 || s1[length1 - 1] == 12288))
            {
                --length1;
            }

            while (length2 > 0 && (s2[length2 - 1] == 32 || s2[length2 - 1] == 12288))
            {
                --length2;
            }

            return CultureInfo.CurrentCulture.CompareInfo.Compare(s1, 0, length1, s2, 0, length2, s_compareFlags);
        }
        
        internal static int IndexOfString(string s1, string s2) => CultureInfo.CurrentCulture.CompareInfo.IndexOf(s1, s2, s_compareFlags);

        internal static bool IsSuffixString(string s1, string s2) => CultureInfo.CurrentCulture.CompareInfo.IsSuffix(s1, s2, s_compareFlags);
    }
}