using System.Collections.Generic;

namespace Brudixy.Interfaces
{
    public interface IDataContainerXPropertyChangedArgs
    {
        bool IsPropertyChanged(string propertyCode);

        T GetNewValue<T>(string propertyCode);

        T GetOldValue<T>(string propertyCode);

        IDataRowContainer Row { get; }

        IEnumerable<string> ChangedPropertyCodes { get; }
    }
}