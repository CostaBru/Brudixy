using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface IReadOnlyDataTable : ICoreReadOnlyDataTable
    {
        /// <summary>
        /// Get all rows using field and passed value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [CanBeNull]
        new IEnumerable<IDataTableReadOnlyRow> GetRows<T>(string column, T value) where T : IComparable;
        
        /// <summary>
        /// Gets first row using data column and passed value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnHandle"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [CanBeNull]
        IDataTableReadOnlyRow GetRow<T>(int columnHandle, T value) where T : IComparable;
        
        /// <summary>
        /// Get all rows using data column and passed value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnHandle"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [CanBeNull]
        IEnumerable<IDataTableReadOnlyRow> GetRows<T>(int columnHandle, T value) where T : IComparable;
        
        /// <summary>
        /// Get all rows where passed data column is null.
        /// </summary>
        /// <param name="columnHandle"></param>
        /// <returns></returns>
        [NotNull]
        new IEnumerable<IDataTableReadOnlyRow> GetRowsWhereNull(int columnHandle);
        
        /// <summary>
        /// Gets all data columns.
        /// </summary>
        [NotNull]
        new IEnumerable<IDataTableReadOnlyColumn> Columns { get; }
        
        /// <summary>
        /// Makes a copy of this table.
        /// </summary>
        /// <returns></returns>
        [NotNull]
        new IReadOnlyDataTable Copy();
        
        /// <summary>
        /// Gets data column by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [NotNull]
        new IDataTableReadOnlyColumn GetColumn(string name);

        /// <summary>
        /// Tries to get data column by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [CanBeNull]
        new IDataTableReadOnlyColumn TryGetColumn(string name);
        
        /// <summary>
        /// Selects rows using string filter passed.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [NotNull]
        IEnumerable<IDataTableReadOnlyRow> Select([NotNull] string filter);
        
        /// <summary>
        /// Gets tables collection.
        /// </summary>
        [NotNull]
        new IEnumerable<IReadOnlyDataTable> Tables { get; }

        /// <summary>
        /// Gets table if exist or null.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [CanBeNull]
        new IReadOnlyDataTable TryGetTable(string tableName);

        /// <summary>
        ///  Gets table if exist or exception.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [NotNull]
        new IReadOnlyDataTable GetTable(string tableName);
    }
}