using System.Collections.Generic;

namespace Lungfetcher.Data
{
    [System.Serializable]
    public class Entry
    {
        public long id;
        public string uuid;
        public string custom_key;
        public string note;
        public string helper_image_url;
        public long word_count;
        public long character_count;
        public List<Localization> localizations;
    }
}