namespace Lungfetcher.Data
{
    [System.Serializable]
    public class TableType
    {
        public int id;
        public string name;
    }

    public enum TableTypeEnum
    {
        Text = 1,
        Image = 2,
        Audio = 3,
        Video = 4,
    }
}