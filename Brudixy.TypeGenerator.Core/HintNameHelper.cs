using System;
using System.IO;

namespace Brudixy.TypeGenerator.Core
{
    internal static class HintNameHelper
    {
        /// <summary>
        /// Roslyn requires <c>hintName</c> passed to <c>GeneratorExecutionContext.AddSource</c> to be a relative, file-like name.
        /// Paths (especially absolute Unix-like paths that start with '/') will fail on non-Windows OSes.
        /// </summary>
        internal static string Sanitize(string hintName, string fallbackBaseName = "Brudixy")
        {
            if (string.IsNullOrWhiteSpace(hintName))
            {
                return fallbackBaseName + ".g.cs";
            }

            var result = hintName.Trim();

            // Strip leading separators to avoid invalid segments like "/..." on Unix.
            result = result.TrimStart('/', '\\');

            // Convert any directory separators into a safe character.
            result = result.Replace('/', '_').Replace('\\', '_');

            // Replace any other invalid filename chars.
            foreach (var ch in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(ch, '_');
            }

            if (string.IsNullOrWhiteSpace(result))
            {
                result = fallbackBaseName;
            }

            // Keep whatever extension the generator intended. Only add .g.cs when there's no extension.
            // Roslyn doesn't require a specific extension, but using .g.cs helps IDE tooling.
            if (Path.GetExtension(result).Length == 0)
            {
                result += ".g.cs";
            }

            return result;
        }
    }
}
