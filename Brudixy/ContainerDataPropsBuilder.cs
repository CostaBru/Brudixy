using Konsarpoo.Collections;

namespace Brudixy;

public class ContainerDataPropsBuilder : CoreContainerDataPropsBuilder
{
    public IEnumerable<(string type, object value)> RowAnnotations;
    public Map<string, Map<string, object>> CellAnnotations;
    public Map<string, Map<string, object>> XPropInfo;

    public override CoreContainerDataProps ToProps()
    {
        return new ContainerDataProps(RowHandle, Data, DataRowState, DisplayDateTimeUtcOffsetTicks, ExtProperties, OriginalData, Age, RowAnnotations, CellAnnotations, XPropInfo);
    }
}