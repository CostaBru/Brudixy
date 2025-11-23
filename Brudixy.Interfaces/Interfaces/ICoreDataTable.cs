using System;
using System.Collections.Generic;
using System.Xml.Linq;
using JetBrains.Annotations;


namespace Brudixy.Interfaces
{
 public interface ICoreDataTable : ICoreReadOnlyDataTable, IDisposable
 {
  /// <summary>
  /// Starts transaction.
  /// </summary>
  IDataEditTransaction StartTransaction();

  /// <summary>
  /// Gets or sets table name.
  /// </summary>
  new string TableName { get; set; }

  /// <summary>
  /// Adds new row to the table.
  /// </summary>
  /// <param name="rowAccessor"></param>
  /// <returns></returns>
  [CanBeNull]
  ICoreDataRowAccessor AddRow([NotNull] ICoreDataRowReadOnlyAccessor rowAccessor);

  /// <summary>
  /// Creates new row. New row is not belongs to the table. All table events are detached from this row.
  /// </summary>
  /// <param name="values"></param>
  /// <returns></returns>
  [NotNull]
  ICoreDataRowContainer NewRow(IReadOnlyDictionary<string, object> values = null);

  /// <summary>
  /// Gets row by field name and passed value.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="value"></param>
  /// <returns></returns>
  [CanBeNull]
  new ICoreDataRowAccessor GetRowBy<T>(T value) where T : IComparable;

  /// <summary>
  /// Gets row by its handle\index.
  /// </summary>
  /// <returns>Can be null if row with given handle was removed.</returns>
  [CanBeNull]
  new ICoreDataRowAccessor GetRow(int rowHandle);

  /// <summary>
  /// Gets row by field name and passed value.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="column"></param>
  /// <param name="value"></param>
  /// <returns></returns>
  [CanBeNull]
  new ICoreDataRowAccessor GetRow<T>([NotNull] string column, T value) where T : IComparable;

  /// <summary>
  /// Gets first row using data column and passed value.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="columnHandle"></param>
  /// <param name="value"></param>
  /// <returns></returns>
  [CanBeNull]
  new ICoreDataRowAccessor GetRow<T>([NotNull] int columnHandle, T value) where T : IComparable;

  /// <summary>
  /// Get all rows using field and passed value.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="column"></param>
  /// <param name="value"></param>
  /// <returns></returns>
  [CanBeNull]
  new IEnumerable<ICoreDataRowAccessor> GetRows<T>([NotNull] string column, T value) where T : IComparable;

  /// <summary>
  /// Get all rows using data column and passed value.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="columnHandle"></param>
  /// <param name="value"></param>
  /// <returns></returns>
  [CanBeNull]
  new IEnumerable<ICoreDataRowAccessor> GetRows<T>([NotNull] int columnHandle, T value) where T : IComparable;

  /// <summary>
  /// Get all rows where passed field is null.
  /// </summary>
  /// <param name="column"></param>
  /// <returns></returns>
  [NotNull]
  new IEnumerable<ICoreDataRowAccessor> GetRowsWhereNull([NotNull] string column);

  /// <summary>
  /// Get all rows where passed data column is null.
  /// </summary>
  /// <param name="columnHandle"></param>
  /// <returns></returns>
  [NotNull]
  new IEnumerable<ICoreDataRowAccessor> GetRowsWhereNull([NotNull] int columnHandle);

  /// <summary>
  /// Gets all rows that are present in table.
  /// </summary>
  [NotNull]
  new IEnumerable<ICoreDataRowAccessor> AllRows { get; }

  /// <summary>
  /// Gets rows if row state is not deleted.
  /// </summary>
  [NotNull]
  new IEnumerable<ICoreDataRowAccessor> Rows { get; }

  /// <summary>
  /// Gets all data columns.
  /// </summary>
  [NotNull]
  new IDataColumnCollection<ICoreDataTableColumn> Columns { get; }

  /// <summary>
  /// Gets data column by name.
  /// </summary>
  /// <param name="name"></param>
  /// <returns></returns>
  [NotNull]
  new ICoreDataTableColumn GetColumn(string name);

  /// <summary>
  /// Tries to get data column by name.
  /// </summary>
  /// <param name="name"></param>
  /// <returns></returns>
  [CanBeNull]
  new ICoreDataTableColumn TryGetColumn(string name);

  /// <summary>
  /// Load rows to the table.
  /// </summary>
  /// <param name="dataRows"></param>
  /// <param name="overrideExisting"></param>
  void LoadRows([NotNull] IEnumerable<ICoreDataRowReadOnlyAccessor> dataRows, bool overrideExisting = false);

  /// <summary>
  /// Setups unchanged row state and clears history.
  /// </summary>
  void AcceptChanges();

  /// <summary>
  /// Rejects changes for all rows.
  /// </summary>
  void RejectChanges();

  /// <summary>
  /// Makes a clone of this table without data.
  /// </summary>
  /// <returns></returns>
  [NotNull]
  ICoreDataTable Clone();

  /// <summary>
  /// Makes a copy of this table.
  /// </summary>
  /// <returns></returns>
  [NotNull]
  new ICoreDataTable Copy();

  /// <summary>
  /// Merges table data using passed table as the source.
  /// </summary>
  /// <param name="source"></param>
  void MergeData([NotNull] ICoreDataTable source);

  /// <summary>
  /// Merges table metadata using passed table as the source.
  /// </summary>
  /// <param name="source"></param>
  void MergeMeta([NotNull] ICoreDataTable source);

  /// <summary>
  /// Performs the full merge using passed table as the source.
  /// </summary>
  /// <param name="source"></param>
  void Merge([NotNull] ICoreDataTable source);

  /// <summary>
  /// Clears data.
  /// </summary>
  void ClearRows();

  /// <summary>
  /// Imports new row to the table.
  /// </summary>
  /// <param name="row"></param>
  /// <returns></returns>
  ICoreDataRowAccessor ImportRow([NotNull] ICoreDataRowReadOnlyAccessor row);

  /// <summary>
  /// Adds new column to the table.
  /// </summary>
  ICoreDataTableColumn AddColumn([NotNull] string columnName,
   TableStorageType valueType = TableStorageType.String,
   TableStorageTypeModifier valueTypeModifier = TableStorageTypeModifier.Simple,
   Type userType = null,
   bool? autoIncrement = null,
   bool? unique = null,
   uint? columnMaxLength = null,
   object defaultValue = null);

  /// <summary>
  /// Adds column to the table.
  /// </summary>
  /// <param name="column"></param>
  /// <returns></returns>
  ICoreDataTableColumn AddColumn([NotNull] ICoreTableReadOnlyColumn column);

  /// <summary>
  /// Gets or sets owner of the table.
  /// </summary>
  new IDataOwner Owner { get; set; }


  /// <summary>
  /// Silently sets the value for all rows. No events to be raised.
  /// </summary>
  /// <param name="columnOrXProp"></param>
  /// <param name="value"></param>
  void SilentlySetValue([NotNull] string columnOrXProp, object value);

  /// <summary>
  /// Silently sets the value for all rows. No events to be raised.
  /// </summary>
  /// <param name="columnHandle"></param>
  /// <param name="value"></param>
  void SilentlySetValue([NotNull] int columnHandle, object value);

  /// <summary>
  /// Removes the row from the table.
  /// </summary>
  /// <param name="row"></param>
  void RemoveRow([NotNull] ICoreDataRowAccessor row);

  /// <summary>
  /// Clears all rows and columns except builtin.
  /// </summary>
  void ClearColumns();

  /// <summary>
  /// Deserializes table from XElement.
  /// </summary>
  /// <param name="source"></param>
  void LoadFromXml([NotNull] XElement source);

  /// <summary>
  /// Deserializes data from XElement.
  /// </summary>
  /// <param name="source"></param>
  void LoadDataFromXml([NotNull] XElement source);

  /// <summary>
  /// Read metadata from XElement.
  /// </summary>
  /// <param name="schema"></param>
  void LoadMetadataFromXml(XElement schema);

  /// <summary>
  /// Deserializes table from Json.
  /// </summary>
  /// <param name="source"></param>
  void LoadFromJson([NotNull] JElement source);

  /// <summary>
  /// Deserializes data from Json.
  /// </summary>
  /// <param name="source"></param>
  void LoadDataFromJson([NotNull] JElement source);

  /// <summary>
  /// Deserializes meta from Json.
  /// </summary>
  /// <param name="source"></param>
  void LoadMetadataFromJson([NotNull] JElement source);

  /// <summary>
  /// Removes particular column from the table.
  /// </summary>
  /// <param name="column"></param>
  bool RemoveColumn([NotNull] int column);

  /// <summary>
  /// Removes particular column from the table.
  /// </summary>
  /// <param name="column"></param>
  bool RemoveColumn([NotNull] string column);

  /// <summary>
  /// Adds new index to the table.
  /// </summary>
  /// <param name="columnHandle"></param>
  /// <param name="unique"></param>
  void AddIndex([NotNull] int columnHandle, bool unique = false);

  /// <summary>
  /// Adds new index to the table.
  /// </summary>
  /// <param name="column"></param>
  /// <param name="unique"></param>
  void AddIndex([NotNull] string column, bool unique = false);

  /// <summary>
  /// Gets copy of table with changed rows only.
  /// </summary>
  /// <returns></returns>
  [NotNull]
  ICoreDataTable GetChanges();

  /// <summary>
  /// Begins load table. Does not aggregate events.
  /// </summary>
  IDataLoadState BeginLoad();

  /// <summary>
  /// Sets table extended property.
  /// </summary>
  /// <param name="propertyName"></param>
  /// <param name="value"></param>
  void SetXProperty<T>([NotNull] string propertyName, [CanBeNull] T value);

  /// <summary>
  /// Removes index.
  /// </summary>
  /// <param name="column"></param>
  /// <returns></returns>
  bool RemoveIndex(string column);

  /// <summary>
  /// Removes index.
  /// </summary>
  /// <param name="columnHandle"></param>
  /// <returns></returns>
  bool RemoveIndex(int columnHandle);

  /// <summary>
  /// Adds multi column index.
  /// </summary>
  /// <param name="columns"></param>
  /// <param name="unique"></param>
  void AddMultiColumnIndex(IEnumerable<string> columns, bool unique = false);

  /// <summary>
  /// Removes multi column index.
  /// </summary>
  /// <param name="columns"></param>
  void RemoveMultiColumnIndex(IEnumerable<string> columns);

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

  /// <summary>
  /// Gets a flag indicating table has index for field passed.
  /// </summary>
  /// <param name="column"></param>
  /// <returns></returns>
  bool HasIndex([NotNull] string column);

  /// <summary>
  /// Gets a flag indicating table has index for data column passed.
  /// </summary>
  /// <param name="columnHandle"></param>
  /// <returns></returns>
  bool HasIndex([NotNull] int columnHandle);

  /// <summary>
  /// Gets tables collection.
  /// </summary>
  [NotNull]
  new IEnumerable<ICoreDataTable> Tables { get; }

  /// <summary>
  /// Gets table if exist or null.
  /// </summary>
  /// <param name="tableName"></param>
  /// <returns></returns>
  [CanBeNull]
  new ICoreDataTable TryGetTable(string tableName);

  /// <summary>
  ///  Gets table if exist or exception.
  /// </summary>
  /// <param name="tableName"></param>
  /// <returns></returns>
  [NotNull]
  new ICoreDataTable GetTable(string tableName);

  /// <summary>
  /// Creates new table.
  /// </summary>
  /// <param name="tableName"></param>
  /// <returns></returns>
  [NotNull]
  ICoreDataTable NewTable(string tableName = null);

  /// <summary>
  /// Removes table from dataset.
  /// </summary>
  /// <param name="tableName"></param>
  void DropTable(string tableName);

  /// <summary>
  /// Clear table data.
  /// </summary>
  void ClearData();

  /// <summary>
  /// Remove data relation from table.
  /// </summary>
  /// <param name="relationName"></param>
  void RemoveRelation(string relationName);

  /// <summary>
  /// Adds new relation to the table.
  /// </summary>
  /// <param name="relationName"></param>
  /// <param name="parentKey"></param>
  /// <param name="childKey"></param>
  /// <param name="relationType"></param>
  /// <param name="constraintUpdate"></param>
  /// <param name="constraintDelete"></param>
  /// <param name="acceptRejectRule"></param>
  /// <returns></returns>
  IDataRelation AddRelation(
   [NotNull] string relationName,
   (string parentTable, string parentColumn) parentKey,
   (string childTable, string childColumn) childKey,
   RelationType relationType = RelationType.OneToMany,
   Rule constraintUpdate = Rule.None,
   Rule constraintDelete = Rule.None,
   AcceptRejectRule acceptRejectRule = AcceptRejectRule.None);

  /// <summary>
  /// Adds new relation to the table.
  /// </summary>
  /// <param name="relationName"></param>
  /// <param name="parentTable"></param>
  /// <param name="childTable"></param>
  /// <param name="key"></param>
  /// <param name="relationType"></param>
  /// <param name="constraintUpdate"></param>
  /// <param name="constraintDelete"></param>
  /// <param name="acceptRejectRule"></param>
  /// <returns></returns>
  IDataRelation AddRelation(
   [NotNull] string relationName,
   [NotNull] string parentTable,
   [NotNull] string childTable,
   [NotNull] IReadOnlyList<(string parentColumn, string childColumn)> key,
   RelationType relationType = RelationType.OneToMany,
   Rule constraintUpdate = Rule.None,
   Rule constraintDelete = Rule.None,
   AcceptRejectRule acceptRejectRule = AcceptRejectRule.None);
 }
}
