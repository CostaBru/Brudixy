using System.IO;

namespace Brudixy.TypeGenerator.Core;

public interface IFileSystemAccessor
{
    string GetFileContents(string path);
}

public class FileSystemAccessor : IFileSystemAccessor
{
    public string GetFileContents(string path)
    {
        return File.ReadAllText(path);
    }
}