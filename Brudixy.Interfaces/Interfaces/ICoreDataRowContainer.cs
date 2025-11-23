using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace Brudixy.Interfaces
{
    public interface ICoreDataRowContainer : ICoreDataRowReadOnlyContainer, ICoreDataRowAccessor
    {
        [CanBeNull]
        new object this[[NotNull] string columnOrXProp] { get; set; }

        [NotNull]
        new ICoreDataRowContainer Set<T>([NotNull] string columnOrXProp, T value);

        [NotNull]
        new ICoreDataRowContainer SetNull([NotNull] string columnOrXProp);

        [CanBeNull]
        new object this[ICoreTableReadOnlyColumn column] { get; set; }

        [NotNull]
        new ICoreDataRowContainer Set<T>([NotNull] ICoreTableReadOnlyColumn column, T value);

        [NotNull]
        new ICoreDataRowContainer SetNull([NotNull] ICoreTableReadOnlyColumn column);

        [CanBeNull]
        new object this[int handle] { get; set; }

        [NotNull]
        new ICoreDataRowReadOnlyContainer ToReadOnly();

        [NotNull]
        new ICoreDataRowContainer Clone();

        XElement ToXml();

        JElement ToJson();

        void SetColumnXProperty<T>([NotNull] string column, [NotNull] string property, [CanBeNull] T value);

        new RowState RowRecordState { get; set; }

        void SetOwner(IDataOwner value);
    }
}