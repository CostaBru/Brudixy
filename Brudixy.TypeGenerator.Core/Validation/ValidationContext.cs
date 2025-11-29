using System.Collections.Generic;
using Brudixy.Interfaces.Generators;

namespace Brudixy.TypeGenerator.Core.Validation
{
    /// <summary>
    /// Provides context information to validation rules during schema validation.
    /// </summary>
    public class ValidationContext
    {
        /// <summary>
        /// Gets the table being validated.
        /// </summary>
        public DataTableObj Table { get; }

        /// <summary>
        /// Gets the file path of the YAML schema being validated.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the dictionary of loaded base tables (key: file path, value: DataTableObj).
        /// </summary>
        public Dictionary<string, DataTableObj> LoadedBaseTables { get; }

        /// <summary>
        /// Gets the file system accessor for reading base table files.
        /// </summary>
        public IFileSystemAccessor FileSystem { get; }

        /// <summary>
        /// Gets or sets additional context data that can be used by validation rules.
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; }

        public ValidationContext(
            DataTableObj table,
            string filePath,
            IFileSystemAccessor fileSystem)
        {
            Table = table;
            FilePath = filePath;
            FileSystem = fileSystem;
            LoadedBaseTables = new Dictionary<string, DataTableObj>();
            AdditionalData = new Dictionary<string, object>();
        }

        public ValidationContext(
            DataTableObj table,
            string filePath,
            IFileSystemAccessor fileSystem,
            Dictionary<string, DataTableObj> loadedBaseTables)
        {
            Table = table;
            FilePath = filePath;
            FileSystem = fileSystem;
            LoadedBaseTables = loadedBaseTables ?? new Dictionary<string, DataTableObj>();
            AdditionalData = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets all available columns including inherited columns from base tables.
        /// </summary>
        public IEnumerable<string> GetAllAvailableColumns()
        {
            var columns = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            // Add columns from current table
            foreach (var col in Table.Columns.Keys)
            {
                columns.Add(col);
            }

            // Add columns from base tables
            foreach (var baseTable in LoadedBaseTables.Values)
            {
                foreach (var col in baseTable.Columns.Keys)
                {
                    columns.Add(col);
                }
            }

            return columns;
        }
    }
}
