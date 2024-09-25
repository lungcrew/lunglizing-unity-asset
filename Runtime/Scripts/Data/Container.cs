namespace Lungfetcher.Data
{
    [System.Serializable]
    public class Container
    {
        public long id;
        public int type_id;
        public string name;
        public string description;
        public ContainerType type;
    }
}