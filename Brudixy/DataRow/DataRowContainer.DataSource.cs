using System.Collections;
using System.ComponentModel;

namespace Brudixy;

public partial class DataRowContainer :    
    INotifyPropertyChanged,
    INotifyPropertyChanging,
    INotifyDataErrorInfo,
    IEditableObject,
    IDataErrorInfo
{
    public event PropertyChangedEventHandler PropertyChanged;
    
    public event PropertyChangingEventHandler PropertyChanging;
    
    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

    public IEnumerable GetErrors(string propertyName)
    {
        var valueOrDefault = GetCellError(propertyName);

        if (valueOrDefault != null)
        {
            yield return valueOrDefault;
        }
    }

    public bool HasErrors => string.IsNullOrEmpty(this.GetRowError()) == false ||  GetErrorColumns().Any();

    string IDataErrorInfo.Error => this.GetRowError();

    string IDataErrorInfo.this[string columnName] => GetCellError(columnName);

    protected virtual void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    protected virtual void OnPropertyChanging(string name)
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(name));
    }

    protected virtual void OnErrorsChanged(string name)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(name));
    }
}