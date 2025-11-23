using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface ICoreReadOnlyDataTable 
    {
        /// <summary>
        /// Gets table primary key.
        /// </summary>
        IEnumerable<ICoreDataTableColumn> PrimaryKey { get; }
        
        /// <summary>
        /// Gets owner of the table.
        /// </summary>
        IDataOwner Owner { get; }

        /// <summary>
        /// Gets columns count.
        /// </summary>
        int ColumnCount { get; }
        
        /// <summary>
        /// Gets row by its handle\index.
        /// </summary>
        /// <returns>Can be null if row with given handle was removed.</returns>
        [CanBeNull] 
        new ICoreDataRowReadOnlyAccessor GetRow(int rowHandle);

        /// <summary>
        /// Gets row by field name and passed value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [CanBeNull]
        ICoreDataRowReadOnlyAccessor GetRow<T>(string column, T value) where T : IComparable;
        
        /// <summary>
        /// Get all rows using field and passed value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [CanBeNull]
        IEnumerable<ICoreDataRowReadOnlyAccessor> GetRows<T>(string column, T value) where T : IComparable;

        /// <summary>
        /// Get all rows where passed field is null.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        [NotNull]
        IEnumerable<ICoreDataRowReadOnlyAccessor> GetRowsWhereNull(string column);

        /// <summary>
        /// Gets all rows that are present in table.
        /// </summary>
        [NotNull]
        IEnumerable<ICoreDataRowReadOnlyAccessor> AllRows { get; }

        /// <summary>
        /// Gets all data columns.
        /// </summary>
        [NotNull]
        IEnumerable<ICoreTableReadOnlyColumn> Columns { get; }

        /// <summary>
        /// Gets rows if row state is not deleted.
        /// </summary>
        [NotNull]
        IEnumerable<ICoreDataRowReadOnlyAccessor> Rows { get; }

        /// <summary>
        /// Gets a flag indicating weather table has nested relations.
        /// </summary>
        bool HasNestedRelations { get; }

        /// <summary>
        /// Gets table name.
        /// </summary>
        string TableName { get; }

        /// <summary>
        /// Gets all rows count.
        /// </summary>
        int RowCount { get; }

        /// <summary>
        /// Makes a copy of this table.
        /// </summary>
        /// <returns></returns>
        [NotNull]
        ICoreReadOnlyDataTable Copy();

        /// <summary>
        /// Returns a flag indicating table has passed column name.
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        bool HasColumn(string columnName);

        /// <summary>
        /// Gets data column by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [NotNull]
        ICoreTableReadOnlyColumn GetColumn(string name);


        /// <summary>
        /// Tries to get data column by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [CanBeNull]
        ICoreTableReadOnlyColumn TryGetColumn(string name);


        /// <summary>
        /// Returns a flag indicating table has changes.
        /// </summary>
        /// <returns></returns>
        bool HasChanges();

        /// <summary>
        /// Returns a flag indicating table has no changes.
        /// </summary>
        /// <returns></returns>
        bool IsNotChanged();

        /// <summary>
        /// Serializes table to XElement.
        /// </summary>
        /// <param name="writeMode"></param>
        /// <returns></returns>
        [NotNull]
        XElement ToXml(SerializationMode writeMode = SerializationMode.DataOnly);

        /// <summary>
        /// Serializes table to JSON.
        /// </summary>
        /// <param name="writeMode"></param>
        /// <returns></returns>
        [NotNull]
        JElement ToJson(SerializationMode writeMode = SerializationMode.DataOnly);

        /// <summary>
        /// Gets table extended property.
        /// </summary>
        /// <param name="xPropertyName"></param>
        /// <param name="original"></param>
        /// <returns></returns>
        [CanBeNull]
        T GetXProperty<T>([NotNull] string xPropertyName, bool original = false);

        /// <summary>
        /// Gets the list of table's extended properties.
        /// </summary>
        [NotNull]
        IEnumerable<string> XProperties { get; }
        
        /// <summary>
        /// Gets max value of field passed.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [CanBeNull]
        IComparable Max([NotNull] string columnName, Tuple<string, IComparable> filter = null);


        /// <summary>
        /// Gets min value of field passed.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [CanBeNull]
        IComparable Min([NotNull] string columnName, Tuple<string, IComparable> filter = null);
        
        bool HasTable(string tableName);

        bool ContainsRelation([NotNull] string relationName);

        IEnumerable<IDataRelation> Relations { get; }

        [CanBeNull]
        IDataRelation TryGetDataRelation(string relationName);
        
        int TablesCount { get; }
        
        [NotNull]
        IEnumerable<ICoreReadOnlyDataTable> Tables { get; }

        [CanBeNull]
        ICoreReadOnlyDataTable TryGetTable(string tableName);

        [NotNull]
        ICoreReadOnlyDataTable GetTable(string tableName);
    }
}