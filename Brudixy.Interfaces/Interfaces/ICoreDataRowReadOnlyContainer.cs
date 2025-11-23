using System;
using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface ICoreDataRowReadOnlyContainer : ICoreDataRowReadOnlyAccessor, IDisposable
    {
        [NotNull]
        new ICoreDataRowReadOnlyContainer Clone();
        
        IDataOwner GetOwner();
    }
}