using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface IDataTable : ICoreDataTable, IReadOnlyDataTable
    {
        /// <summary>
        /// Gets all rows that are present in table.
        /// </summary>
        [NotNull]
        new IDataTableRowEnumerable<IDataTableRow> AllRows { get; }

        /// <summary>
        /// Gets rows if row state is not deleted.
        /// </summary>
        [NotNull]
        new IDataTableRowEnumerable<IDataTableRow> Rows { get; }

        /// <summary>
        /// Get all rows using field and passed value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [CanBeNull]
        new IEnumerable<IDataTableRow> GetRows<T>([NotNull] string column, T value) where T : IComparable;

        /// <summary>
        /// Get all rows where passed field is null.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        [NotNull]
        new IEnumerable<IDataTableRow> GetRowsWhereNull([NotNull] string column);

        /// <summary>
        /// Creates new row. New row is not belongs to the table. All table events are detached from this row.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        [NotNull]
        new IDataRowContainer NewRow(IReadOnlyDictionary<string, object> values = null);

        /// <summary>
        /// Adds new row to the table.
        /// </summary>
        /// <param name="rowAccessor"></param>
        /// <returns></returns>
        [CanBeNull]
        new IDataTableRow AddRow([NotNull] IDataRowReadOnlyAccessor rowAccessor);

        /// <summary>
        /// Gets row by first unique index.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        [CanBeNull]
        new IDataTableRow GetRowBy<T>(T value) where T : IComparable;

        /// <summary>
        /// Gets table primary key.
        /// </summary>
        new IEnumerable<IDataTableColumn> PrimaryKey { get; }

        /// <summary>
        /// Gets row by field name and passed value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [CanBeNull]
        new IDataTableRow GetRow<T>([NotNull] string column, T value) where T : IComparable;

        /// <summary>
        /// Selects rows using string filter passed.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        new IEnumerable<IDataTableRow> Select([NotNull] string filter);

        /// <summary>
        /// Gets first row using data column and passed value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnHandle"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [CanBeNull]
        new IDataTableRow GetRow<T>(int columnHandle, T value) where T : IComparable;

        /// <summary>
        /// Get all rows using data column and passed value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnHandle"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [CanBeNull]
        new IEnumerable<IDataTableRow> GetRows<T>(int columnHandle, T value)
            where T : IComparable;

        /// <summary>
        /// Get all rows where passed data column is null.
        /// </summary>
        /// <param name="columnHandle"></param>
        /// <returns></returns>
        [NotNull]
        new IEnumerable<IDataTableRow> GetRowsWhereNull(int columnHandle);

        /// <summary>
        /// Gets all data columns.
        /// </summary>
        [NotNull]
        new IDataColumnCollection<IDataTableColumn> Columns { get; }

        /// <summary>
        /// Gets data column by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [NotNull]
        new IDataTableColumn GetColumn(string name);

        /// <summary>
        /// Tries to get data column by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [CanBeNull]
        new IDataTableColumn TryGetColumn(string name);

        /// <summary>
        /// Creates new Columns. This column is not attached to the table yet.
        /// </summary>
        /// <returns></returns>
        new IDataTableColumn NewColumn();

        /// <summary>
        /// Adds new column to the table.
        /// </summary>
        IDataTableColumn AddColumn([NotNull] string columnName,
            TableStorageType valueType = TableStorageType.String,
            TableStorageTypeModifier valueTypeModifier = TableStorageTypeModifier.Simple,
            Type type = null,
            string displayName = null,
            bool? autoIncrement = null,
            bool? readOnly = null,
            bool? unique = null,
            string dataExpression = null,
            uint? columnMaxLength = null,
            object defaultValue = null,
            bool builtin = false,
            bool serviceColumn = false,
            bool allowNull = false,
            IReadOnlyDictionary<string, object> xProps = null);

        /// <summary>
        /// Adds column to the table.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        IDataTableColumn AddColumn([NotNull] IDataTableReadOnlyColumn column);

        /// <summary>
        /// Resets all aggregated events.
        /// </summary>
        void ResetAggregatedEvents();

        /// <summary>
        /// Row column changed weak event. Suspended by Begin init\edit. Aggregated.
        /// </summary>
        [NotNull]
        IDataColumnChangedDataEvent ColumnChanged { get; }

        /// <summary>
        /// Row column changing weak event. Suspended by Begin init\edit. 
        /// </summary>
        [NotNull]
        IDataColumnChangingDataEvent ColumnChanging { get; }

        /// <summary>
        /// Row added weak event.
        /// </summary>
        [NotNull]
        IDataRowAddedDataEvent RowAdded { get; }

        /// <summary>
        /// Row adding weak event.
        /// </summary>
        [NotNull]
        IDataRowAddingDataEvent RowAdding { get; }

        /// <summary>
        /// Row deleted weak event.
        /// </summary>
        [NotNull]
        IDataRowDeletedDataEvent RowDeleted { get; }

        /// <summary>
        /// Row deleting weak event.
        /// </summary>
        [NotNull]
        IDataRowDeletingDataEvent RowDeleting { get; }

        /// <summary>
        /// Row changed weak event.
        /// </summary>
        [NotNull]
        IDataRowChangedDataEvent DataRowChanged { get; }

        /// <summary>
        /// Max string length constraint raised weak event.
        /// </summary>
        [NotNull]
        IMaxColumnLenConstraintDataEvent MaxColumnLenConstraint { get; }

        /// <summary>
        /// Table disposed weak event.
        /// </summary>
        [NotNull]
        IDataTableDisposedDataEvent Disposed { get; }

        /// <summary>
        /// Row extended property changing event.
        /// </summary>
        [NotNull]
        IDataRowXPropertyChangingDataEvent RowXPropertyChanging { get; }

        /// <summary>
        /// Row extended property changed weak event.
        /// </summary>
        [NotNull]
        IDataRowXPropertyChangedDataEvent RowXPropertyChanged { get; }

        /// <summary>
        /// Table extended property changed event.
        /// </summary>
        [NotNull]
        IDataTableXPropertyChangedDataEvent XPropertyChanged { get; }

        /// <summary>
        /// Table extended property changing event.
        /// </summary>
        [NotNull]
        IDataTableXPropertyChangingDataEvent XPropertyChanging { get; }

        /// <summary>
        /// DataRow meta data changed event.
        /// </summary>
        IDataRowMetaDataChangedEvent RowMetaDataChangedEvent { get; }

        /// <summary>
        /// Subscribes particular column changing.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="eventHandler"></param>
        /// <param name="context">Subscription context being sent to event handler.</param>
        /// <returns></returns>
        bool SubscribeColumnChanging<T>([NotNull] string columnName, [NotNull] Action<IDataColumnChangingTypedEventArgs<T>, string> eventHandler, string context = null);

        /// <summary>
        /// Unsubscribe particular column changing.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="eventHandler"></param>
        /// <returns></returns>
        bool UnsubscribeColumnChanging<T>([NotNull] string columnName, [NotNull] Action<IDataColumnChangingTypedEventArgs<T>, string> eventHandler);

        /// <summary>
        /// Subscribes particular column changed.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="eventHandler"></param>
        /// <param name="context">Subscription context being sent to event handler.</param>
        /// <returns></returns>
        bool SubscribeColumnChanged([NotNull] string columnName, [NotNull] Action<IDataColumnChangedEventArgs, string> eventHandler, string context = null);

        /// <summary>
        /// Unsubscribes particular column changed.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="eventHandler"></param>
        /// <returns></returns>
        bool UnsubscribeColumnChanged([NotNull] string columnName, [NotNull] Action<IDataColumnChangedEventArgs, string> eventHandler);

        /// <summary>
        /// Subscribes particular column new row value requesting.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="eventHandler"></param>
        /// <param name="context">Subscription context being sent to event handler.</param>
        /// <returns></returns>
        bool SubscribeCellValueRequesting([NotNull] string columnName, [NotNull] Func<INewRowCellValueRequestingArgs, string, bool> eventHandler, string context = null);

        /// <summary>
        /// Unsubscribes particular column new row value requesting.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="eventHandler"></param>
        /// <returns></returns>
        bool UnsubscribeCellValueRequesting([NotNull] string columnName, [NotNull] Func<INewRowCellValueRequestingArgs, string, bool> eventHandler);

        /// <summary>
        /// Locks all raising events
        /// </summary>
        /// <returns></returns>
        IDataLockEventState LockEvents();

        /// <summary>
        /// Creates a deep copy of a table.
        /// </summary>
        /// <returns></returns>
        new IDataTable Copy();

        /// <summary>
        /// Creates a deep copy of table metadata only.
        /// </summary>
        /// <returns></returns>
        new IDataTable Clone();

        /// <summary>
        /// Gets copy of table with changed rows only.
        /// </summary>
        /// <returns></returns>
        [NotNull]
        new IDataTable GetChanges();

        /// <summary>
        /// Creates a dedicated row for editing in a sandbox.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public IDataTableRow BeginEditRow(IDataTableRow row);

        /// <summary>
        /// Ends an editing in row in a sandbox.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public IDataTableRow EndEditRow(IDataTableRow row);

        /// <summary>
        /// Cancels an editing in row in a sandbox.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public IDataTableRow CancelEditRow(IDataTableRow row);

        /// <summary>
        /// Cancels all edit for all rows in table.
        /// </summary>
        void CancelEdit();

        /// <summary>
        /// Ends all edit for all rows in table.
        /// </summary>
        void EndEdit();

        /// <summary>
        /// Gets tables collection.
        /// </summary>
        [NotNull]
        new IEnumerable<IDataTable> Tables { get; }

        /// <summary>
        /// Gets table if exist or null.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [CanBeNull]
        new IDataTable TryGetTable(string tableName);

        /// <summary>
        ///  Gets table if exist or exception.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [NotNull]
        new IDataTable GetTable(string tableName);

        /// <summary>
        /// Creates new table.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [NotNull]
        new IDataTable NewTable(string tableName = null);
        
        /// <summary>
        /// Adds new table.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [NotNull]
        IDataTable AddTable(string tableName);
    }
}