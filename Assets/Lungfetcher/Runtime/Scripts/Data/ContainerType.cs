namespace Lungfetcher.Data
{
    [System.Serializable]
    public class ContainerType
    {
        public int id;
        public string name;
    }

    public enum ContainerTypeEnum
    {
        Text = 1,
        Image = 2,
        Audio = 3,
        Video = 4,
    }
}