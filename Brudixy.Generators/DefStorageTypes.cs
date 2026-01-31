using System.Collections.Generic;
using Brudixy.Interfaces.Generators;
using Microsoft.CodeAnalysis;

namespace Brudixy.Generators
{
    internal static class DefStorageTypes
    {
        public static IReadOnlyList<StorageType> GetStorageTypes(GeneratorExecutionContext context)
        {
            return BuiltinSupportStorageTypes.StorageTypes;
        }
    }
}