using Brudixy.Interfaces.Delegates;
using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface IDataRowReadOnlyContainer : ICoreDataRowReadOnlyContainer, IDataRowReadOnlyAccessor
    {
        [NotNull]
        IDataEvent<IDataContainerFieldValueChangingArgs> FieldValueChangingEvent { get; }

        [NotNull]
        IDataEvent<IDataContainerFieldValueChangedArgs> FieldValueChangedEvent { get; }

        [NotNull]
        IDataEvent<IDataContainerXPropertyChangedArgs> XPropertyChangedEvent { get; }

        [NotNull]
        IDataEvent<IDataContainerXPropertyChangingArgs> XPropertyChangingEvent { get; }
        
        [NotNull]
        IDataEvent<IDataContainerMetaDataChangedArgs> MetaDataChangedEvent { get; }
        
        [NotNull]
        new IDataRowReadOnlyContainer Clone();
    }
}