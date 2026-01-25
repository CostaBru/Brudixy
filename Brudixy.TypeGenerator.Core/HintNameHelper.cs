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

            // Preserve well-known generated suffixes before sanitizing.
            // (Roslyn hintName is not a path; suffixes matter mainly for predictable persisted filenames.)
            var endsWithGeneratedCs = result.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
            var endsWithGeneratedRowInterfaces = result.EndsWith(".RowInterfaces.g.cs", StringComparison.OrdinalIgnoreCase);

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

            // Re-apply expected suffixes if sanitization altered them.
            // This specifically prevents cases where CI persists files as `*.cs` instead of `*.g.cs`.
            if (endsWithGeneratedRowInterfaces && !result.EndsWith(".RowInterfaces.g.cs", StringComparison.OrdinalIgnoreCase))
            {
                // Strip any trailing .cs/.g.cs that may remain, then append canonical suffix.
                result = Path.GetFileNameWithoutExtension(result);
                if (result.EndsWith(".RowInterfaces", StringComparison.OrdinalIgnoreCase))
                {
                    result = result.Substring(0, result.Length - ".RowInterfaces".Length);
                }
                result += ".RowInterfaces.g.cs";
                return result;
            }

            if (endsWithGeneratedCs && !result.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            {
                // Ensure we keep `.g.cs` for generated files.
                result = Path.GetFileNameWithoutExtension(result) + ".g.cs";
                return result;
            }

            // If there's no extension at all, add .g.cs for IDE tooling.
            if (Path.GetExtension(result).Length == 0)
            {
                result += ".g.cs";
            }

            return result;
        }
    }
}
