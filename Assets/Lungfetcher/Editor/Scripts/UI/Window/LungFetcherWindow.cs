using System.Collections.Generic;
using System.Linq;
using Lungfetcher.Editor.Scriptables;
using Lungfetcher.Editor.Scriptables.Settings;
using Lungfetcher.Editor.UI.Elements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Lungfetcher.Helper.Logger;

namespace Lungfetcher.Editor.UI.Window
{
    public class LungFetcherWindow : EditorWindow
    {
        private const string TreeAssetPath = "UI Documents/LungFetcherWindow";
        private VisualTreeAsset _visualTreeAsset;
        //private List<ProjectSo> _projectSoList = new List<ProjectSo>();
        //private List<string> _projectNames = new List<string>();
        private ProjectSo _currentProject;
        private DropdownField _projectsDropdown;
        private ProjectElement _projectElement;
        private VisualElement _projectRoot;
        private VisualElement _containerRoot;
        private Button _addProjectButton;
        private Button _reloadProjectsButton;
        private Button _syncContainersButton;
        private Button _hardSyncContainersButton;
        private Dictionary<long, ContainerElement> _containerElements = new Dictionary<long, ContainerElement>();
        private Dictionary<string, ProjectSo> _projectSoDic = new Dictionary<string, ProjectSo>();
        private LungSettings Settings => LungSettings.instance;

        [MenuItem("Tools/LungFetcher/LungFetcherWindow")]
        public static void OpenWindow()
        {
            LungFetcherWindow wnd = GetWindow<LungFetcherWindow>();
            wnd.titleContent = new GUIContent("LungFetcherWindow");
        }

        public void CreateGUI()
        {
            _visualTreeAsset = Resources.Load<VisualTreeAsset>(TreeAssetPath);
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Clone UXML so the split view has height
            _visualTreeAsset.CloneTree(root);

            SetReferencesFromXml(root);
            SetButtonsListeners();

            SetupProjects();
            FillContainers();

            _projectsDropdown.RegisterValueChangedCallback(ProjectDropdownValueChanged);
        }

        private void SetReferencesFromXml(VisualElement root)
        {
            _projectsDropdown = root.Q<DropdownField>("projects-dropdown");
            _projectRoot = root.Q<VisualElement>("project-root-element");
            _containerRoot = root.Q<VisualElement>("container-root-element");
            _addProjectButton = root.Q<Button>("add-project-btn");
            _reloadProjectsButton = root.Q<Button>("reload-projects-btn");
            _syncContainersButton = root.Q<Button>("sync-containers-btn");
            _hardSyncContainersButton = root.Q<Button>("hardsync-containers-btn");
        }

        private void SetupProjects()
        {
            ToggleSyncButtons(enable:false);
            SetProjectsList();
            FillProjectsDropdown();
        }

        private void ReloadProjects()
        {
            SetupProjects();
        }

        private void ToggleSyncButtons(bool enable)
        {
            _hardSyncContainersButton.SetEnabled(enable);
            _syncContainersButton.SetEnabled(enable);
        }

        private void SaveNewProject()
        {
            var path = EditorUtility.SaveFilePanel("New Project",
                Application.dataPath + Settings.ProjectPath, "LungProject", "asset");

            var savePath = path.Length > 0 ? path[(Application.dataPath.Length)..] : "";

            if (string.IsNullOrEmpty(savePath)) return;

            var projectSo = CreateInstance<ProjectSo>();
            Settings.ProjectPath = savePath;
            var assetPath = $"Assets{Settings.ProjectPath}";
            AssetDatabase.CreateAsset(projectSo, assetPath);
            AssetDatabase.SaveAssetIfDirty(projectSo);

            AddProject(projectSo);
        }

        private void AddProject(ProjectSo projectSo)
        {
            if (!_projectSoDic.TryAdd(projectSo.name, projectSo))
            {
                Logger.LogError($"Duplicate project SO name {projectSo.name}, rename it", 
                    projectSo);
                return;
            }
            
            var stringEntry = string.IsNullOrEmpty(projectSo.ProjectInfo.tag)
                ? projectSo.name
                : projectSo.ProjectInfo.tag;
            _projectsDropdown.choices.Add(stringEntry);
            _projectsDropdown.index = _projectsDropdown.choices.Count - 1;
        }

        private void SetProjectsList()
        {
            var allProjectSoGuids = AssetDatabase.FindAssets("t:ProjectSo");

            _projectSoDic = new Dictionary<string, ProjectSo>();
            
            foreach (var guid in allProjectSoGuids)
            {
                var projectSoAsset = AssetDatabase.LoadAssetAtPath<ProjectSo>(AssetDatabase.GUIDToAssetPath(guid));

                if (_projectSoDic.TryAdd(projectSoAsset.name, projectSoAsset)) continue;
                
                Logger.LogError($"Duplicate project SO name {projectSoAsset.name}, rename it", 
                    projectSoAsset);
            }
        }

        private void FillContainers()
        {
            if (!_currentProject) return;

            foreach (var containerId in _currentProject.ContainerSoDic.Keys)
            {
                if (_containerElements.ContainsKey(containerId)) continue;
                var containerElement = new ContainerElement(_currentProject.ContainerSoDic[containerId]);
                _containerElements.Add(containerId, containerElement);
                _containerRoot.Add(containerElement);
                containerElement.OnElementAutoRemoved += ContainerRemoved;
            }
        }

        private void ClearContainers()
        {
            foreach (var containerElement in _containerElements.Values)
            {
                containerElement.Cleanup();
                _containerRoot.Remove(containerElement);
                containerElement.OnElementAutoRemoved -= ContainerRemoved;
            }

            _containerElements.Clear();
        }

        private void ContainerRemoved(long containerId)
        {
            if (!_containerElements.TryGetValue(containerId, out var containerElement)) return;

            _containerElements.Remove(containerId);
        }

        private void FillProjectsDropdown()
        {
            if (_projectsDropdown == null) return;

            _projectsDropdown.choices.Clear();

            foreach (var projectName in _projectSoDic.Keys)
            {
                _projectsDropdown.choices.Add(projectName);
            }
            _projectsDropdown.choices.Sort();

            _projectsDropdown.index = -1;
        }

        private void ProjectDropdownValueChanged(ChangeEvent<string> evt)
        {
            ClearContainers();
            ToggleSyncButtons(enable:false);

            if (_projectElement != null && _projectRoot.Contains(_projectElement))
                _projectRoot.Remove(_projectElement);
            if (_projectsDropdown.index == -1)
            {
                _projectElement?.Cleanup();
                _currentProject = null;
            }
            else
            {
                var projectSo = _projectSoDic[_projectsDropdown.value];
                if (!projectSo)
                {
                    _projectSoDic.Remove(_projectsDropdown.value);
                    _projectsDropdown.choices.RemoveAt(_projectsDropdown.index);
                    _projectsDropdown.SetValueWithoutNotify("");
                    _currentProject = null;
                    return;
                }

                _currentProject = _projectSoDic[_projectsDropdown.value];

                if (_projectElement == null)
                    CreateProjectElement();
                else
                {
                    _projectElement.ReloadElement(_currentProject);
                }

                _projectRoot.Add(_projectElement);

                FillContainers();
            }
        }

        private void CreateProjectElement()
        {
            _projectElement = new ProjectElement(_currentProject);
            _projectElement.OnProjectNull += CurrentProjectRemoved;
            _projectElement.OnProjectUpdated += FillContainers;
            _projectElement.OnToggleSyncContainersButton += ToggleSyncButtons;
            ToggleSyncButtons(_projectElement.SyncButtonEnabled);
        }

        private void CurrentProjectRemoved()
        {
            _projectSoDic.Remove(_projectsDropdown.value);
            _projectsDropdown.choices.RemoveAt(_projectsDropdown.index);
            _currentProject = null;
            _projectsDropdown.index = -1;
        }
        
        private void SyncContainers() => _projectElement?.SyncContainers(hardSync: false);

        private void HardSyncContainers() => _projectElement?.SyncContainers(hardSync: true);
        
        private void SetButtonsListeners()
        {
            _addProjectButton.clicked += SaveNewProject;
            _reloadProjectsButton.clicked += ReloadProjects;
            _syncContainersButton.clicked += SyncContainers;
            _hardSyncContainersButton.clicked += HardSyncContainers;
        }

        private void ClearButtonListeners()
        {
            _addProjectButton.clicked -= SaveNewProject;
            _reloadProjectsButton.clicked -= ReloadProjects;
            _syncContainersButton.clicked -= SyncContainers;
            _hardSyncContainersButton.clicked -= HardSyncContainers;
        }

        private void Cleanup()
        {
            _projectElement?.Cleanup();
            foreach (var containerElement in _containerElements.Values)
            {
                containerElement.OnElementAutoRemoved -= ContainerRemoved;
                containerElement.Cleanup();
            }

            ClearButtonListeners();

            if (_projectElement == null) return;
            _projectElement.OnProjectNull -= CurrentProjectRemoved;
            _projectElement.OnProjectUpdated -= FillContainers;
            _projectElement.OnToggleSyncContainersButton -= ToggleSyncButtons;
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
