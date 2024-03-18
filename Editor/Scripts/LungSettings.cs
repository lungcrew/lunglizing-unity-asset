using System.Linq;
using Lungfetcher.Data;
using Lungfetcher.Helper;
using Lungfetcher.Editor.Scriptables;
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

        public void ReceiveProject(Project projectInfo)
        {
            if (_projectDic.Count <= 0)
            {
                _projectDic.Add(projectInfo.id, CreateProject(projectInfo));
            }
            else
            {
                if (_projectDic.TryGetValue(projectInfo.id, out var scriptable))
                {
                    if (scriptable == null)
                    {
                        _projectDic[projectInfo.id] = CreateProject(projectInfo);
                    }
                    else
                    {
                        ProjectSo projectSo = (ProjectSo)scriptable;
                        projectSo.SyncProjectInfo(projectInfo);
                    }
                }
                else
                {
                    _projectDic.Add(projectInfo.id, CreateProject(projectInfo));
                }
            }
        }

        public void ReceiveTables(List<Table> tables)
        {
            if(CurrentProject)
                CurrentProject.UpdateAllTables(tables);

            else
            {
                Debug.LogError("No Project Selected");
            }
        }

        public void ReceiveTable(Table table)
        {
            if (CurrentProject)
            {
                CurrentProject.UpdateTable(table);
            }
            else
            {
                Debug.LogError("No Project Selected");
            }
        }
        
        public void SelectProject(long id)
        {
            if (_projectDic.TryGetValue(id, out var scriptable))
            {
                CurrentProject = (ProjectSo)scriptable;
            }
            else
            {
                DebugLog.ProjectNotFound(id);
            }
        }

        private ProjectSo CreateProject(Project projectInfo)
        {
            ProjectSo projectSo = CreateInstance<ProjectSo>();
            AssetDatabase.CreateAsset(projectSo, $"{_newProjectPath}/{projectInfo.title}.asset");
            
            projectSo.SyncProjectInfo(projectInfo);
            
            return projectSo;
        }

        public void RemoveProject(long id)
        {
            if (_projectDic.TryGetValue(id, out var scriptable))
            {
                if (scriptable)
                {
                    ProjectSo projectSo = (ProjectSo)scriptable;
                    projectSo.RemoveAllTables();
                    string path = AssetDatabase.GetAssetPath(projectSo);
                    AssetDatabase.DeleteAsset(path);
                }
                _projectDic.Remove(id);
            }
            else
            {
                DebugLog.ProjectNotFound(id);
            }
        }

        public void RemoveAllProjects()
        {
            List<KeyValuePair<long,ScriptableObject>> projectDicTempList = _projectDic.ToList();

            foreach (var valuePair in projectDicTempList)
            {
                RemoveProject(valuePair.Key);
            }
        }
    }

}