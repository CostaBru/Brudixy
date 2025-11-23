using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Brudixy.Interfaces;
using Brudixy.Interfaces.Delegates;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy;

public class DataTableView :
    INotifyCollectionChanged,
    INotifyPropertyChanged,
    INotifyPropertyChanging,
    IEditableObject,
    IBindingList, 
    IDataEventReceiver<IDataRowDeletedArgs>,
    IDataEventReceiver<IDataTableRowAddedArgs>,
    IDataEventReceiver<IDataTableXPropertyChangedArgs>, 
    IDataEventReceiver<IDataTableXPropertyChangingArgs>, 
    IDataEventReceiver<IDataTableTransactionRollbackEventArgs>,
    IDataEventReceiver<IDataColumnChangedEventArgs>
{
    private readonly WeakReference<DataTable> m_reference;
    private readonly string m_tableName;
    
    private Data<WeakReference<IDataEditTransaction>> m_transactionReferences = new ();
    
    private string m_filter;
    
    private string m_sortKey = string.Empty;
    
    private ListSortDirection m_direction = ListSortDirection.Ascending;

    private Data<int> m_order;

    protected DataTable Table
    {
        get
        {
            if (m_reference.TryGetTarget(out var tbl))
            {
                return tbl;
            }

            throw new ObjectDisposedException($"DataTableView {m_tableName} is disposed.");
        }
    }

    public DataTableView([NotNull] DataTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException(nameof(table));
        }
        
        m_reference = new WeakReference<DataTable>(table);
        
        table.RowDeleted.SubscribeTarget(this);
        table.RowAdded.SubscribeTarget(this);
        table.XPropertyChanged.SubscribeTarget(this);
        table.XPropertyChanging.SubscribeTarget(this);
        table.TransactionRollback.SubscribeTarget(this);
        table.ColumnChanged.SubscribeTarget(this);
    }

    public event NotifyCollectionChangedEventHandler CollectionChanged;
    public event PropertyChangedEventHandler PropertyChanged;
    public event PropertyChangingEventHandler PropertyChanging;
    public event ListChangedEventHandler ListChanged;
    
    public int Count => Table.RowCount;

    public bool IsSynchronized => false;

    public object SyncRoot => null;

    public bool IsFixedSize => false;

    public bool IsReadOnly => Table.IsReadOnly;

    public object this[int index]
    {
        get
        {
            if (m_order != null)
            {
                return Table.GetRow(m_order[index]);
            }
            else
            {
                
                return Table[index];
            }
        }
        set
        {
            if (Table.PrimaryKey.Any())
            {
                if (value is ICoreDataRowReadOnlyAccessor cn)
                {
                    Table.ImportRow(cn);
                    
                    return;
                }
            }
            
            throw new NotSupportedException();
        }
    }

    public void BeginEdit()
    {
        var transactionReference = new WeakReference<IDataEditTransaction>(Table.StartTransaction());
        
        m_transactionReferences.Push(transactionReference);
    }

    public void CancelEdit()
    {
        if (m_transactionReferences.Count == 0)
        {
            return;
        }

        var transactionReference = m_transactionReferences.Pop();

        if (transactionReference.TryGetTarget(out var tran))
        {
            tran.Rollback();
        }
    }

    public void EndEdit()
    {
        if (m_transactionReferences.Count == 0)
        {
            return;
        }
        
        var transactionReference = m_transactionReferences.Pop();

        if (transactionReference.TryGetTarget(out var tran))
        {
            tran.Commit();
        }
    }

    public IEnumerator GetEnumerator()
    {
        var dataTable = Table;
        
        if (m_order != null)
        {
            return m_order.Select(rh => dataTable.GetRow(rh)).GetEnumerator();
        }
        
        return dataTable.Rows.GetEnumerator();
    }

    public void CopyTo([NotNull] Array array, int index)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (index <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        
        if (array.Length + index > Table.RowCount)
        {
            throw new ArgumentOutOfRangeException();
        }

        foreach (var row in Table.Rows)
        {
            array.SetValue(row, index);

            index++;
        }
    }

    public int Add(object value)
    {
        if (value is ICoreDataRowReadOnlyAccessor cn)
        {
            Table.AddRow(cn);

            return Count - 1;
        }

        throw new NotSupportedException();
    }

    public void Clear()
    {
        Table.ClearRows();
        
        m_order?.Clear();
    }

    public bool Contains(object value)
    {
        if (value is ICoreDataRowReadOnlyAccessor cn)
        {
            return IndexOf(cn) == 0;
        }
        
        throw new NotSupportedException();
    }

    public int IndexOf(object value)
    {
        if (value is ICoreDataRowReadOnlyAccessor cn)
        {
            var primaryKey = Table.PrimaryKey.ToArray();

            if (primaryKey.Length == 1)
            {
                var comparable = cn.GetRowKeyValue()[0];

                var dataRow = Table.GetRowBySinglePk<IComparable>(comparable);

                if (dataRow == null)
                {
                    return -1;
                }

                return Table.GetRowHandleIndex(dataRow.RowHandleCore);
            }
            else if(primaryKey.Length > 1)
            {
                var comparables = cn.GetRowKeyValue();
                
                var dataRow = Table.GetRowByMultiColPk(comparables);
                
                if (dataRow == null)
                {
                    return -1;
                }
                
                return Table.GetRowHandleIndex(dataRow.RowHandleCore);
            }

            var rowIndex = Table.Rows.FindIndexE( cn, (r, c) => DataRowContainer.CompareDataRows(r, c).cmp == 0);
                
            return rowIndex;
        }

        return -1;
    }

    public void Insert(int index, object value)
    {
        Add(value);
    }

    public void Remove(object value)
    {
        if (value is ICoreDataRowReadOnlyAccessor cn)
        {
            Table.DeleteRow(cn.RowHandle);
        }
        
        throw new NotSupportedException();
    }

    public void RemoveAt(int index)
    {
        var dataRow = Table[index];

        Table.DeleteRow(dataRow.RowHandle);
    }

    public void AddIndex(PropertyDescriptor property)
    {
        Table.AddIndex(property.Name);
    }

    public object AddNew()
    {
        return Table.NewRow();
    }

    public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
    {
        var propertyName = property?.Name ?? string.Empty;

        ApplySort(direction, propertyName);
    }

    private void ApplySort(ListSortDirection direction, string propertyName)
    {
        m_sortKey = propertyName;
        m_direction = direction;

        if (direction == ListSortDirection.Ascending)
        {
            m_order = this.Table.Rows.OrderBy(m_sortKey).Select(r => r.RowHandleCore).ToData();
        }
        else
        {
            m_order = this.Table.Rows.OrderByDescending(m_sortKey).Select(r => r.RowHandleCore).ToData();
        }
    }

    public int Find(PropertyDescriptor property, object key)
    {
        var dataRow = Table.GetRow(property.Name, (IComparable)key);

        if (dataRow != null)
        {
            return dataRow.RowHandle;
        }

        return -1;
    }

    public void RemoveIndex(PropertyDescriptor property)
    {
        Table.RemoveIndex(property.Name);
    }

    public void RemoveSort()
    {
        m_sortKey = string.Empty;
        m_order?.Dispose();
        m_order = null;
    }

    public bool AllowEdit { get; protected set; }

    public bool AllowNew { get; protected set; }

    public bool AllowRemove { get; protected set; }

    public bool IsSorted { get; protected set; }

    public ListSortDirection SortDirection => m_direction;
    public PropertyDescriptor SortProperty => m_sortKey.Length == 0 ? null : new DataTableColumnPropertyDescriptor(m_sortKey);

    public bool SupportsChangeNotification { get; protected set; }

    public bool SupportsSearching { get; protected set; }

    public bool SupportsSorting { get; protected set; }
    
    public bool OnEvent(IDataRowDeletedArgs args, string context = null)
    {
        RemoveDeletedRow(args);
        OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, 0));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, args.DeletedRows));

        return false;
    }

    public bool OnEvent(IDataTableRowAddedArgs args, string context = null)
    {
        InsertNewRow(args);
        OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, args.Row.RowHandle));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, args.Rows));
        
        return false;
    }
  
    public bool OnEvent(IDataTableXPropertyChangedArgs args, string context = null)
    {
        OnPropertyChanged(new PropertyChangedEventArgs(args.XPropertyName));
        OnListChanged(new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, 0));
        
        return false;
    }

    public bool OnEvent(IDataTableXPropertyChangingArgs args, string context = null)
    {
        OnPropertyChanging(new PropertyChangingEventArgs(args.XPropertyName));

        return false;
    }
  
    public bool OnEvent(IDataTableTransactionRollbackEventArgs args, string context = null)
    {
        InvalidateSort();

        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, 0));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

        return false;
    }
    
    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(this, e);
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }

    protected virtual void OnPropertyChanging(PropertyChangingEventArgs e)
    {
        PropertyChanging?.Invoke(this, e);
    }

    protected virtual void OnListChanged(ListChangedEventArgs e)
    {
        ListChanged?.Invoke(this, e);
    }
    
    private void InsertNewRow(IDataTableRowAddedArgs args)
    {
        if (string.IsNullOrEmpty(m_sortKey))
        {
            return;
        }

        if (m_direction == ListSortDirection.Ascending)
        {
            var comparables = m_order
                .Select(r => Table.GetRow(r))
                .SelectFieldValue<IComparable>(m_sortKey)
                .ToData();

            var comparable = args.Row.Field<IComparable>(m_sortKey);

            int ascDirectionInsertionIndex = 0;

            if (comparable != null)
            {
                comparables
                    .BinarySearchExact(comparable,
                        0,
                        comparables.Length,
                        (r, targetVal) => r.CompareTo(targetVal));

                if (ascDirectionInsertionIndex < 0)
                {
                    ascDirectionInsertionIndex = ~ascDirectionInsertionIndex;
                }
            }

            m_order.Insert(ascDirectionInsertionIndex, args.Row.RowHandle);
        }
        else
        {
            var comparables = m_order
                .Select(r => Table.GetRow(r))
                .Reverse()
                .SelectFieldValue<IComparable>(m_sortKey)
                .ToData();

            var comparable = args.Row.Field<IComparable>(m_sortKey);

            int descInsertionIndex = m_order.Length;

            if (comparable != null)
            {
                var insertionIndex = comparables
                    .BinarySearchExact(comparable,
                        0,
                        comparables.Length,
                        (r, targetVal) => r.CompareTo(targetVal));

                if (insertionIndex < 0)
                {
                    insertionIndex = ~insertionIndex;
                }

                descInsertionIndex = m_order.Length - insertionIndex;
            }

            m_order.Insert(descInsertionIndex, args.Row.RowHandle);
        }
    }
    
    private void RemoveDeletedRow(IDataRowDeletedArgs args)
    {
        if (string.IsNullOrEmpty(m_sortKey))
        {
            return;
        }

        foreach (var row in args.DeletedRows)
        {
            m_order.Remove(row.RowHandle);
        }
    }
    
    private void InvalidateSort()
    {
        if (string.IsNullOrEmpty(m_sortKey))
        {
            return;
        }

        ApplySort(m_direction, m_sortKey);
    }

    bool IDataEventReceiver<IDataColumnChangedEventArgs>.OnEvent(IDataColumnChangedEventArgs args, string context = null)
    {
       if(args.ChangedColumnNames.Contains(m_sortKey))
       {
           InvalidateSort();
       }

       return false;
    }
}

public class DataTableColumnPropertyDescriptor : PropertyDescriptor
{
    private readonly string m_name;

    public DataTableColumnPropertyDescriptor(string name) : base(name, null)
    {
        m_name = name;
    }

    public DataTableColumnPropertyDescriptor([NotNull] MemberDescriptor descr, Attribute[] attrs) : base(descr, attrs)
    {
    }

    public DataTableColumnPropertyDescriptor([NotNull] string name, Attribute[] attrs) : base(name, attrs)
    {
    }

    public override bool CanResetValue(object component)
    {
        return true;
    }

    public override object GetValue(object component)
    {
        return (component as ICoreDataRowAccessor)?[m_name];
    }

    public override void ResetValue(object component)
    {
        (component as ICoreDataRowAccessor)?.SetNull(m_name);
    }

    public override void SetValue(object component, object value)
    {
        if (component is ICoreDataRowAccessor dr)
        {
            dr[m_name] = value;
        }
    }

    public override bool ShouldSerializeValue(object component)
    {
        return false;
    }

    public override Type ComponentType => typeof(ICoreDataRowAccessor);
    public override bool IsReadOnly => false;

    public override Type PropertyType => typeof(object);
}