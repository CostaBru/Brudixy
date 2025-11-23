using System.Collections.Generic;
using Brudixy.Interfaces.Generators;
using Microsoft.CodeAnalysis;

namespace Brudixy.Generators
{
    public static class DefStorageTypes
    {
        public static IReadOnlyList<StorageType> GetStorageTypes(GeneratorExecutionContext context)
        {
            return TableStorageTypeGenerator.StorageTypes;
        }
    }
}