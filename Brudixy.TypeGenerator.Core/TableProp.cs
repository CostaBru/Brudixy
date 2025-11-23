namespace Brudixy.TypeGenerator.Core
{
    public class TableProp
    {
        public string CodeProperty { get; set; }
        public string FileName { get; set; }

        public void EnsureDefaults(string tableName)
        {
            if (string.IsNullOrEmpty(FileName))
            {
                FileName = tableName;
            }
            
            if (string.IsNullOrEmpty(CodeProperty))
            {
                CodeProperty = tableName;
            }
        }
    }
}