using System.IO.Compression;
using Konsarpoo.Collections;
using Konsarpoo.Collections.Persistence;

namespace Brudixy.Persistense;

public class FileRandomAccessTransactionData<T, TChange> : RandomAccessTransactionData<T, TChange>
{
    private FileData<T> m_storage;
    
    public void OpenOrCreate(string storageFile, int maxSizeOfArray, byte[] key, CompressionLevel compressionLevel)
    {
        m_storage = File.Exists(storageFile) ? FileData<T>.Open(storageFile, key, compressionLevel) : FileData<T>.Create(storageFile, maxSizeOfArray, key, compressionLevel);
    }

    public override IRandomAccessTransactionData<T, TChange> Copy()
    {
        throw new NotSupportedException();
    }

    public override IRandomAccessTransactionData<T, TChange> Clone()
    {
        throw new NotSupportedException();
    }

    protected override IRandomAccessData<T> CreateStorage()
    {
        if (m_storage != null)
        {
            return m_storage;
        }
        
        throw new NotSupportedException("Storage not opened. Call OpenOrCreate first.");
    }
}