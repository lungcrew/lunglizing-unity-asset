using Lungfetcher.Data;
using UnityEngine;
using UnityEngine.Localization;

namespace Lungfetcher.Editor.Scriptables
{
	[System.Serializable]
	public class LocaleField
	{
		[SerializeField]private Locale locale;

		public string code;
		public long id;
		public string name;

		public Locale Locale => locale;
		
		public void SetLocale(Locale loc)
		{
			locale = loc;
		}

		public LocaleField(ProjectLocale projectLocale)
		{
			name = projectLocale.name;
			id = projectLocale.id;
			code = projectLocale.code;
		}

		public void UpdateLocaleSoftData(ProjectLocale projectLocale)
		{
			name = projectLocale.name;
			code = projectLocale.code;
		}
	}
}