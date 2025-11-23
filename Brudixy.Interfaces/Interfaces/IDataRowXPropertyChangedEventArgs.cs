using System.Collections.Generic;

namespace Brudixy.Interfaces
{
    public interface IDataRowXPropertyChangedEventArgs : IDataTableEventArgs
    {
        bool IsPropertyChanged(string propertyCode);

        T GetNewValue<T>(string columnName);

        T GetOldValue<T>(string columnName);

        IDataTableRow Row { get; }

        IEnumerable<string> ChangedPropertyCodes { get; }
    }
}