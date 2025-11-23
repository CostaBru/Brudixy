using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface IDataRowReadOnlyAccessor : ICoreDataRowReadOnlyAccessor
    {
        new string GetTableName();
        
        [NotNull]
        string ToString(string columnOrXProp, string format = null, IFormatProvider formatProvider = null);

        [NotNull]
        new IEnumerable<IDataTableReadOnlyColumn> PrimaryKeyColumn { get; }

        [NotNull]
        new IDataTableReadOnlyColumn GetColumn([NotNull] string columnName);
        
        [NotNull]
        new IDataTableReadOnlyColumn GetColumn(int columnHandle);

        [CanBeNull]
        new IDataTableReadOnlyColumn TryGetColumn([NotNull] string columnName);

        [NotNull]
        new string ToString(IDataTableReadOnlyColumn column, string format = null, IFormatProvider formatProvider = null);

        [NotNull]
        new IEnumerable<IDataTableReadOnlyColumn> GetColumns();
       
        [CanBeNull]
        new T Field<T>([NotNull] IDataTableReadOnlyColumn column);

        [CanBeNull]
        new T Field<T>([NotNull] IDataTableReadOnlyColumn column, T defaultIfNull);

        new bool IsNull([NotNull] IDataTableReadOnlyColumn columnName);

        new bool IsNotNull([NotNull] IDataTableReadOnlyColumn columnName);

        [CanBeNull]
        new object this[[NotNull] IDataTableReadOnlyColumn column] { get; }

        [CanBeNull]
        new object GetOriginalValue([NotNull] IDataTableReadOnlyColumn column);

        new T GetOriginalValue<T>([NotNull] IDataTableReadOnlyColumn column);
        
        [CanBeNull]
        object SilentlyGetValue([NotNull] string columnName);

        [CanBeNull]
        string GetCellError([NotNull] string columnName);

        [CanBeNull]
        string GetCellWarning([NotNull] string columnName);

        [CanBeNull]
        string GetCellInfo([NotNull] string columnName);
        
        [CanBeNull]
        T GetCellAnnotation<T>([NotNull] string column, [NotNull] string type);
        
        [NotNull]
        IEnumerable<(string type, object value)> GetCellAnnotations([NotNull] string columnName);
        
        [NotNull]
        IEnumerable<(string column, string type, object value)> GetCellAnnotations();

        [CanBeNull]
        string GetRowFault();
        
        [CanBeNull]
        string GetRowError();

        [CanBeNull]
        string GetRowWarning();

        [CanBeNull]
        string GetRowInfo();
        
        [CanBeNull]
        T GetRowAnnotation<T>([NotNull] string type);
        
        [NotNull]
        IEnumerable<(string type, object value)> GetRowAnnotations();
        
        [CanBeNull]
        IReadOnlyDictionary<string, object> GetXPropertyAnnotationValues([CanBeNull] string propertyCode);
        
        [CanBeNull]
        T GetXPropertyAnnotation<T>([CanBeNull] string propertyCode, [NotNull] string key);
        
        [NotNull]
        IEnumerable<string> XPropertyAnnotations { get; }

        uint GetAnnotationAge();
    }
}