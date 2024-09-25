using System.Collections.Generic;

namespace Lungfetcher.Data
{
    [System.Serializable]
    public class EntriesLocale
    {
        public ProjectLocale locale;
        public List<LocalizedEntry> localizations;
    }
}