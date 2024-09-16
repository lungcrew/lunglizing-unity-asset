using System.Collections.Generic;

namespace Lungfetcher.Data
{
    [System.Serializable]
    public class Project
    {
        public long id;
        public long mother_locale_id;
        public string tag;
        public string description;
        public long entries_count;
        public ProjectLocale mother_locale;
        public List<ProjectLocale> locales;
    }
}