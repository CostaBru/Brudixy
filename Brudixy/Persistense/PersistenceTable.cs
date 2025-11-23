using System.IO.Compression;
using System.Xml.Linq;
using Brudixy.Interfaces;
using Brudixy.Persistense;

namespace Brudixy.Persistence;

public class PersistenceTable : DataTable
{
    public PersistenceTable()
    {
        ComplexDataType = typeof(PersistenceComplexItem<>);
        CommonDataType = typeof(PersistenceDataItem<>);
    }

    private string m_path;
    private byte[] m_key;
    private CompressionLevel m_compressionLevel;
    
    private PersistenceStateInfo m_persistenceStateInfo;

    public void OpenOrCreate(string path, byte[] key, CompressionLevel compressionLevel)
    {
        var combine = Path.Combine(path, Name);
        
        Directory.CreateDirectory(combine);

        m_path = combine;
        m_key = key;
        m_compressionLevel = compressionLevel;
        
        m_persistenceStateInfo = new PersistenceStateInfo(this);
        
        m_persistenceStateInfo.OpenOrCreate(m_path, m_key, m_compressionLevel);
        
        var fileName = GetMetaFileName();

        if (File.Exists(fileName))
        {
            var xElement = XElement.Load(fileName);

            LoadMetadataFromXml(xElement);
        }
        else
        {
            PersistMeta();
        }
       
        IndexInfo.RebuildIndex(this);
        MultiColumnIndexInfo.RebuildIndexes(this);
    }

    private string GetMetaFileName()
    {
        return Path.Combine(m_path, "meta.xml");
    }

    private void PersistMeta()
    {
        var xml = ToXml(SerializationMode.SchemaOnly);

        xml.Save(Path.Combine(m_path, "meta.xml"));
    }

    public string StoragePath => m_path;

    protected override void OnMetadataChanged()
    {
        base.OnMetadataChanged();
        
        PersistMeta();
    }

    protected override StateInfo CreateStateInfo()
    {
        if (m_persistenceStateInfo != null)
        {
            return m_persistenceStateInfo;
        }
        
        throw new InvalidOperationException("Should open or create file storage first.");
    }

    protected override void ConnectDataItem(IDataItem dataItem, CoreDataColumn column)
    {
        if (dataItem is IPersistenceDataItem p)
        {
            p.Connect(this, (DataColumn)column);
            
            return;
        }
        
        throw new NotSupportedException("Only IPersistenceDataItem is supported in PersistenceTable.");
    }
    
    private (string filName, byte[] key, CompressionLevel compLevel, int maxArraySize) GetColumnPersistenceInfo(DataColumn column)
    {
        return (Path.Combine(m_path, column.ColumnName), m_key, m_compressionLevel, 4096);
    }

    public interface IPersistenceDataItem
    {
        void Connect(PersistenceTable dataTable, DataColumn column);
        void BeginWrite();
        void Flush();
        void EndWrite();
    }
    
    internal class PersistenceDataItem<T> : DataItem<T>, IPersistenceDataItem
    {
        public void Connect(PersistenceTable dataTable, DataColumn column)
        {
            var persistenceTable = dataTable;
            
            var (filName, key, compLevel, maxArraySize) = persistenceTable.GetColumnPersistenceInfo(column);

            var fileRandomAccessTransactionData = new FileRandomAccessTransactionData<T, T>();
            
            fileRandomAccessTransactionData.OpenOrCreate(filName, maxArraySize, key, compLevel);
            
            this.Storage = fileRandomAccessTransactionData;
        }
        
        public void BeginWrite()
        {
            if (this.Storage is IFileData fd)
            {
                fd.BeginWrite();
            }
        }

        public void Flush()
        {
            if (this.Storage is IFileData fd)
            {
                fd.Flush();
            }
        }

        public void EndWrite()
        {
            if (this.Storage is IFileData fd)
            {
                fd.EndWrite();
            }
        }

        public override void Dispose(ICoreDataTableColumn column)
        {
            if (this.Storage is IFileData fd)
            {
                fd.Flush();
            }
            
            base.Dispose(column);
        }
    }

    internal class PersistenceComplexItem<T> : ComplexTypeDataItem<T>, IPersistenceDataItem where T : class, ICloneable, IXmlSerializable, IJsonSerializable, new()
    {
        public void Connect(PersistenceTable dataTable, DataColumn column)
        {
            var persistenceTable = dataTable;
            
            var (filName, key, compLevel, maxArraySize) = persistenceTable.GetColumnPersistenceInfo(column);
            
            var fileRandomAccessTransactionData = new FileRandomAccessTransactionData<T, T>();
            
            fileRandomAccessTransactionData.OpenOrCreate(filName, maxArraySize, key, compLevel);

            this.Storage = fileRandomAccessTransactionData;
        }

        public void BeginWrite()
        {
            if (this.Storage is IFileData fd)
            {
                fd.BeginWrite();
            }
        }

        public void Flush()
        {
            if (this.Storage is IFileData fd)
            {
                fd.Flush();
            }
        }

        public void EndWrite()
        {
            if (this.Storage is IFileData fd)
            {
                fd.EndWrite();
            }
        }
        
        public override void Dispose(ICoreDataTableColumn column)
        {
            if (this.Storage is IFileData fd)
            {
                fd.Flush();
            }
            
            base.Dispose(column);
        }
    }
}