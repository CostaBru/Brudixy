using Brudixy.Interfaces;

namespace Brudixy.EventArgs
{
    public class DataContainerXPropertyChangingArgs : IDataContainerXPropertyChangingArgs
    {
        private object m_newValue;
        private object m_oldValue;

        public DataContainerXPropertyChangingArgs(IDataRowContainer rowContainer, string propertyCode, object oldValue, object newValue)
        {
            Row = rowContainer;
            m_newValue = newValue;
            m_oldValue = oldValue;

            PropertyCode = propertyCode;
        }

        public IDataRowContainer Row { get; }

        public string PropertyCode { get; set; }

        public void SetNewValue<T>(T value)
        {
            m_newValue = value;
        }

        public T GetNewValue<T>()
        {
            return XPropertyValueConverter.TryConvert<T>("Row container xprop new value", PropertyCode, m_newValue);
        }

        public T GetOldValue<T>()
        {
            return XPropertyValueConverter.TryConvert<T>("Row container xprop old value", PropertyCode, m_oldValue);
        }

        public bool IsCancel { get; set; }
    }
}