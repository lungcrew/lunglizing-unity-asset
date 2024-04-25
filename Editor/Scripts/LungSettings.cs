using Lungfetcher.Data;
using Lungfetcher.Helper;
using Lungfetcher.Editor.Scriptables;

namespace Lungfetcher.Editor
{
    using UnityEditor;
    using UnityEngine;

    [FilePath("Lungfetcher/Settings.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class LungSettings : ScriptableSingleton<LungSettings>
    {
        [SerializeField]
        private float _test = 42;

        private string _newProjectPath = "Assets/Projects";
        private LongScriptableObjectDictionary _projectDic;
        public ProjectSo CurrentProject { get; private set; }

        public float Test
        {
            get => _test;
            set
            {
                _test = value;
                Save(true);
            }
        }


        private ProjectSo CreateProject(Project projectInfo)
        {
            ProjectSo projectSo = CreateInstance<ProjectSo>();
            AssetDatabase.CreateAsset(projectSo, $"{_newProjectPath}/{projectInfo.title}.asset");
            
            projectSo.SyncProjectInfo(projectInfo);
            
            return projectSo;
        }
        
    }

}