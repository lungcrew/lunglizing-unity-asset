using UnityEngine;

namespace Lungfetcher.Editor
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [FilePath("Lungfetcher/Settings.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class LungSettings : ScriptableSingleton<LungSettings>
    {
        [SerializeField]
        private float _test = 42;

        public float Test
        {
            get => _test;
            set
            {
                _test = value;
                Save(true);
            }
        }
    }

}