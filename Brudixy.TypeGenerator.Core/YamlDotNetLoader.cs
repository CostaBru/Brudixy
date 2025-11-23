using System;
using System.Reflection;
using Brudixy.TypeGenerator.Core.Properties;

namespace Brudixy.TypeGenerator.Core
{
    public static class YamlDotNetLoader
    {
        private static readonly object s_syncRoot = new object();
        private static bool s_isInstalled;
        private static Assembly s_yamlAssembly;

        /// <summary>
        /// Installs the AssemblyResolve handler and ensures the embedded YamlDotNet
        /// assembly is loaded. Safe to call multiple times.
        /// </summary>
        public static void EnsureInstalled()
        {
            if (s_isInstalled)
            {
                return;
            }

            lock (s_syncRoot)
            {
                if (s_isInstalled)
                {
                    return;
                }

                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

                // Pre-load the assembly so we fail fast if the resource is missing or invalid
                var assembly = Assembly.GetExecutingAssembly();
                const string resourceName = "Brudixy.TypeGenerator.Properties.Resources.YamlDotNet";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new InvalidOperationException($"Embedded YamlDotNet resource '{resourceName}' is missing.");
                    }

                    if (s_yamlAssembly == null)
                    {
                        var bytes = new byte[stream.Length];
                        _ = stream.Read(bytes, 0, bytes.Length);
                        s_yamlAssembly = Assembly.Load(bytes);
                    }
                }

                s_isInstalled = true;
            }
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name);

            if (!string.Equals(name.Name, "YamlDotNet", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (s_yamlAssembly != null)
            {
                return s_yamlAssembly;
            }

            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "Brudixy.TypeGenerator.Properties.Resources.YamlDotNet";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                var bytes = new byte[stream.Length];
                _ = stream.Read(bytes, 0, bytes.Length);
                s_yamlAssembly = Assembly.Load(bytes);
                return s_yamlAssembly;
            }
        }
    }
}