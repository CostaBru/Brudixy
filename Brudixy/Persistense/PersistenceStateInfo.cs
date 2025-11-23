using System.IO.Compression;
using Brudixy.Persistence;
using Konsarpoo.Collections;
using Konsarpoo.Collections.Persistence;

namespace Brudixy.Persistense;

public class PersistenceStateInfo : StateInfo
{
    public PersistenceTable Table => (PersistenceTable)DataTable;
    
    public PersistenceStateInfo(CoreDataTable table) : base(table)
    {
    }

    private string m_path;

    private FileSet<int> m_emptyRowSlotsQueueStorage;
    private FileData<int> m_rowHandlesData;
    private FileRandomAccessTransactionData<RowStateInfo, RowStateInfo> m_rowStatesTransactionData;
    private FileRandomAccessTransactionData<Map<string, object>, KeyValuePair<string, object>> m_rowXPropData;
    
    public void OpenOrCreate(string storagePath, byte[] key, CompressionLevel compressionLevel)
    {
        var path = Path.Combine(storagePath, "State");

        m_path = path;

        Directory.CreateDirectory(path);

        var freeHandlesFile = Path.Combine(path, "FreeHandles.bin");
        var rowHandlesFile = Path.Combine(path, "RowHandles.bin");
        var rowStatesFile = Path.Combine(path, "RowStates.bin");
        var rowXpropFile = Path.Combine(path, "RowXProp.bin");

        var maxSizeOfArray = 1024;
        
        m_emptyRowSlotsQueueStorage = FileSet<int>.OpenOrCreate(freeHandlesFile, maxSizeOfArray, key, compressionLevel);
        m_rowHandlesData = File.Exists(rowHandlesFile)
            ? FileData<int>.Open(rowHandlesFile, key, compressionLevel)
            : FileData<int>.Create(rowHandlesFile, maxSizeOfArray, key, compressionLevel);

        m_rowStatesTransactionData = new FileRandomAccessTransactionData<RowStateInfo, RowStateInfo>();
        m_rowStatesTransactionData.OpenOrCreate(rowStatesFile, maxSizeOfArray, key, compressionLevel);

        m_rowXPropData = new FileRandomAccessTransactionData<Map<string, object>, KeyValuePair<string, object>>();
        m_rowXPropData.OpenOrCreate(rowXpropFile, maxSizeOfArray, key, compressionLevel);

        var rowXPropDataItem = RowXProps;
        var rowStateInfoDataItem = RowStates;
        var emptyRowSlotsQueue = EmptyRowSlotsQueue;
        var handles = RowHandles;
    }

    protected override ICollection<int> CreateEmptyRowSlotsQueueStorage(IEnumerable<int> source = null)
    {
        if (m_emptyRowSlotsQueueStorage != null)
        {
            if (source != null)
            {
                m_emptyRowSlotsQueueStorage.Clear();
                m_emptyRowSlotsQueueStorage.AddRange(source);
            }
            
            return m_emptyRowSlotsQueueStorage;
        }
        
        throw new InvalidOperationException("Should open or create file storage first.");
    }

    protected override IRandomAccessData<int> CreateRowHandlesStorage(IEnumerable<int> source = null)
    {
        if (m_rowHandlesData != null)
        {
            if (source != null)
            {
                m_rowHandlesData.Clear();
                m_rowHandlesData.AddRange(source);
            }
            
            return m_rowHandlesData;
        }
        
        throw new InvalidOperationException("Should open or create file storage first.");
    }

    protected override RowStateInfoDataItem CreateRowStatesStorage()
    {
        if(m_rowStatesTransactionData != null)
        {
            return new RowStateInfoDataItem(this.Table) {Storage = m_rowStatesTransactionData };
        }
        
        throw new InvalidOperationException("Should open or create file storage first.");
    }

    protected override RowXPropDataItem CreateRowXPropsStorage()
    {
        if (m_rowXPropData != null)
        {
            return new RowXPropDataItem(this.Table) { Storage = m_rowXPropData };
        }

        throw new InvalidOperationException("Should open or create file storage first.");
    }
}