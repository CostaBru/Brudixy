using Microsoft.CodeAnalysis;

namespace Brudixy.Generators
{
    public class GenerateType
    {
        public GenerateType(string name)
        {
            Name = name;
        }
        
        public string Name { get; set; }
        public string FullName { get; set; }
        public bool IsStruct { get; set; }
        public bool IsNumeric { get; set; }
        public string UniqueIndexClassInit { get; set; }
        public string IndexClassInit { get; set; }
        public bool CustomBinSearch { get; set; }
        public bool OwnCopyClone { get; set; }

        public static GenerateType CreateStructNumericType(string name)
        {
            return new GenerateType(name)
            {
                IsStruct = true,
                IsNumeric = true,
                FullName = "System." + name
            };
        }
        
        public static GenerateType CreateStructType(string name, string fullName = null)
        {
            return new GenerateType(name)
            {
                IsStruct = true,
                FullName = fullName ?? "System." + name
            };
        }

        public static GenerateType[] Types =
        {
            CreateStructNumericType("Int32"),
            CreateStructNumericType("UInt32"),
            CreateStructNumericType("Int16"),
            CreateStructNumericType("UInt16"),
            CreateStructNumericType("Int64"),
            CreateStructNumericType("UInt64"),
            CreateStructNumericType("Single"),
            CreateStructNumericType("Double"),
            CreateStructNumericType("Decimal"),
            CreateStructNumericType("Char"),
            CreateStructNumericType("SByte"),
            CreateStructNumericType("Byte"),
            
            CreateStructType("DateTime"),
            CreateStructType("DateTimeOffset"),
            CreateStructType("TimeSpan"),
            CreateStructType("Boolean"),
            CreateStructType("Guid"),
            
            new GenerateType( "String") { IsStruct = false, IndexClassInit =  "new StringTrieIndex(true, isUnique)", UniqueIndexClassInit = "new StringTrieIndex(true, isUnique)", CustomBinSearch = true, OwnCopyClone = true},
            new GenerateType( "Complex") { UniqueIndexClassInit = "new CoreHashIndex<IComparable>(isUnique)" },
        };
    }

    public class IndexSupportTypes
    {
        public static GenerateType[] GetTypes(GeneratorExecutionContext context)
        {
            return GenerateType.Types;
        }
    }
}