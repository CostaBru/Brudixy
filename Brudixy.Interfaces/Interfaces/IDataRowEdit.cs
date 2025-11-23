using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface IDataRowEdit
    {
        [CanBeNull]
        IDataRowEdit BeginEdit();
  
        [CanBeNull]
        IDataRowContainer Editing { get; }
        
        void EndEdit();

        bool CancelEdit();
    }
}