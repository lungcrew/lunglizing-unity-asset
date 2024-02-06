namespace Lungfetcher.Data
{
    [System.Serializable]
    public class Localization
    {
        public long id;
        public long entry_id;
        public long project_locale_id;
        public string text;
        public string image_url;
        public string audio_url;
        public string video_url;
        public ProjectLocale locale;
    }
}