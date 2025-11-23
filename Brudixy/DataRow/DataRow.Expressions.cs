using Brudixy.Expressions;

namespace Brudixy;

public partial class DataRow 
{
    public bool CheckFilter(string expression, IReadOnlyDictionary<string, object> testing = null)
    {
        var select = new Select(new DataRowExpressionSource(this, testing), expression);
        return select.SelectRows<DataRow>().Any();
    }
}