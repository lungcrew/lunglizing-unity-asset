namespace Lungfetcher.Data
{
    [System.Serializable]
    public class LocalizedEntry
    {
        public long localization_id;
        public long entry_id;
        public string entry_uuid;
        public string entry_readable_key;
        public string text;
        public string image_url;
        public string video_url;
        public string audio_url;
    }
}