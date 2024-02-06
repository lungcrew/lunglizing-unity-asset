using System.Collections.Generic;

namespace Lungfetcher.Data
{
    [System.Serializable]
    public class Project
    {
        public long id;
        public long mother_locale_id;
        public string title;
        public string description;
        public bool test;
        public long entries_count;
        public ProjectLocale mother_locale;
        public List<ProjectLocale> locales;
    }
}