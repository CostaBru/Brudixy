using Brudixy.Converter;
using Brudixy.Interfaces;

namespace Brudixy.EventArgs
{
    public class DataRowXPropertyChangingArgs : IDataRowXPropertyChangingEventArgs
    {
        internal WeakReference<DataTable> Table;

        public string PropertyCode;

        public int RowHandle;

        public object Value;

        public object PrevValue;

        public bool IsCancel;

        string IDataRowXPropertyChangingEventArgs.PropertyCode => PropertyCode;

        void IDataRowXPropertyChangingEventArgs.SetNewValue<T>(T value) => Value = value;

        T IDataRowXPropertyChangingEventArgs.GetNewValue<T>()
        {
            return XPropertyValueConverter.TryConvert<T>("Data row xprop new value", PropertyCode, Value);
        }

        T IDataRowXPropertyChangingEventArgs.GetOldValue<T>()
        {
            return XPropertyValueConverter.TryConvert<T>("Data row xprop old value", PropertyCode, PrevValue);
        }

        IDataTable IDataTableEventArgs.Table => Table.GetReferenceOrDefault();

        IDataTableRow IDataRowXPropertyChangingEventArgs.Row => Table.GetReferenceOrDefault()?.GetRowByHandle(RowHandle);

        bool IDataRowXPropertyChangingEventArgs.IsCancel
        {
            get => IsCancel;
            set => IsCancel = true;
        }
    }
}