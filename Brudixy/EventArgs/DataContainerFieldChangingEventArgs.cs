using Brudixy.Interfaces;

namespace Brudixy.EventArgs
{
    public class DataContainerFieldValueChangingArgs : IDataContainerFieldValueChangingArgs
    {
        public DataContainerFieldValueChangingArgs(IDataRowContainer rowContainer)
        {
            Row = rowContainer;
        }

        public IDataRowContainer Row { get; }

        public string ColumnName { get; set; }

        public object NewValue { get; set; }

        public object OldValue { get; set; }

        public bool IsCancel { get; set; }
    }
}
