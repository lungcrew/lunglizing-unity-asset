using System.Collections.Generic;

namespace Lungfetcher.Data
{
    [System.Serializable]
    public class EntriesLocale
    {
        public long locale_id;
        public string locale_code;
        public string locale_name;
        public List<LocalizedEntry> localizations;
    }
}