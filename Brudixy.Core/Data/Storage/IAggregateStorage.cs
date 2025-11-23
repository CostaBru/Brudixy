using Brudixy.Expressions;
using Konsarpoo.Collections;

namespace Brudixy
{
    internal interface IAggregateStorage
    {
        object GetAggregateValue(Data<int> handles, AggregateType type);
    }
}