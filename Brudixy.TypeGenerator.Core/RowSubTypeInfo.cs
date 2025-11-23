using System.Collections.Generic;

namespace Brudixy.TypeGenerator.Core;

public class RowSubTypeInfo
{
    public string Name { get; set; }

    public string Expression { get; set; }
    
    public Dictionary<string, XProperty> XProperties { get; set; } = new ();
}