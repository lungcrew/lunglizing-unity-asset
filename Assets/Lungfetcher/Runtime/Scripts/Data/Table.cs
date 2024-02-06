namespace Lungfetcher.Data
{
    [System.Serializable]
    public class Table
    {
        public long id;
        public int type_id;
        public string name;
        public string description;
        public TableType type;
    }
}