using UnityEditor;
using UnityEngine;

namespace Lungfetcher.Editor.Scriptables.Settings
{
	[FilePath("Lungfetcher/Settings.asset", FilePathAttribute.Location.PreferencesFolder)]
	public class LungSettings : ScriptableSingleton<LungSettings>
	{
		[SerializeField] private string projectPath;
		public string ProjectPath
		{
			get => projectPath;
			set
			{
				projectPath = value;
				Save(true);
			}
		}
	}
}