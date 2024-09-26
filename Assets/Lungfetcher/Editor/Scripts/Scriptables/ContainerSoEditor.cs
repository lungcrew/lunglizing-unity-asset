using System.Collections.Generic;
using System.Linq;
using Lungfetcher.Data;
using Lungfetcher.Editor.Operations;
using UnityEditor;
using UnityEditor.Localization.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace Lungfetcher.Editor.Scriptables
{
    [CustomEditor(typeof(ContainerSo))]
    public class ContainerSoEditor : UnityEditor.Editor
    {
        #region Fields
        
        private VisualTreeAsset _inspectorXML;
        private ContainerSo _containerSo;
        private List<Container> _projectContainers;
        private List<string> _choices = new List<string>();
        private Label _updateLabel;
        private VisualElement _root;
        private DropdownField _containerDropdown;
        private ScrollView _localesView;
        private ScrollView _progressView;
        private ObjectField _projectObject;
        private Button _syncContainerButton;
        private Button _hardSyncContainerButton;
        
        #endregion

        #region Inspector

        public override VisualElement CreateInspectorGUI()
        {
            // Create a new VisualElement to be the root of our inspector UI
            _root = new VisualElement();

            _inspectorXML = Resources.Load<VisualTreeAsset>("UI Documents/ContainerSoEditor");
            // Load and clone a visual tree from UXML
            _inspectorXML.CloneTree(_root);

            _containerDropdown = _root.Q<DropdownField>("containers-dropdown");
            _updateLabel = _root.Q<Label>("updated-label");
            _localesView = _root.Q<ScrollView>("locales-view");
            _progressView = _root.Q<ScrollView>("progress-view");
            _projectObject = _root.Q<ObjectField>("project-object");
            _syncContainerButton = _root.Q<Button>("sync-container-btn");
            _hardSyncContainerButton = _root.Q<Button>("hard-sync-container-btn");

            if (_syncContainerButton != null)
                _syncContainerButton.clicked += () => SyncContainer();
            
            if(_hardSyncContainerButton != null)
                _hardSyncContainerButton.clicked += () => SyncContainer(true);
            
            _projectObject.schedule.Execute(() => _projectObject.RegisterValueChangedCallback(SwitchProject));
            
            _containerDropdown.RegisterValueChangedCallback(DropdownChanged);

            RefreshContainersDropdown();
            RefreshUpdateLabel();
            RefreshSyncContainerEntriesButtons();
            RefreshEntryFetchProgress();

            // Return the finished inspector UI
            return _root;
        }

        private void SyncContainer(bool hardSync = false)
        {
            _containerSo.FetchEntries(hardSync);
            RefreshEntryFetchProgress();
            RefreshSyncContainerEntriesButtons();
        }

        private void RefreshContainersDropdown()
        {
            if (_containerDropdown == null || _containerSo == null) return;
            
            if (_containerSo.Project)
            {
                if (_containerSo.Project.ContainerList.Count > 0)
                {
                    _projectContainers = _containerSo.Project.ContainerList;
                    _choices = _projectContainers.Select(x => x.name).ToList();
                    _containerDropdown.choices = _choices;

                    if (_containerSo.ContainerInfo != null)
                    {
                        var selectedContainer = _projectContainers.Find(x => x.id == _containerSo.ContainerInfo.id);
                        if (selectedContainer != null)
                        {
                            _containerDropdown.SetValueWithoutNotify(selectedContainer.name);
                        }
                        else
                        {
                            _containerSo.ChangeContainerInfo(null);
                            _containerDropdown.SetValueWithoutNotify(null);
                        }
                    }
                    else
                    {
                        _containerDropdown.SetValueWithoutNotify(null);
                    }
                }
                else
                {
                    _containerSo.ChangeContainerInfo(null);
                    _containerDropdown.choices.Clear();
                    _containerDropdown.SetValueWithoutNotify(null);
                }
            }
            else
            {
                _containerSo.ChangeContainerInfo(null);
                _containerDropdown.choices.Clear();
                _containerDropdown.SetValueWithoutNotify(null);
            }
        }

        private void DropdownChanged(ChangeEvent<string> evt)
        {
            if (_containerDropdown == null) return;

            if (_containerDropdown.index == -1 || _containerDropdown.value == null) return;

            var selectedContainer = _projectContainers[_containerDropdown.index];

            _containerSo.ChangeContainerInfo(selectedContainer);
            RefreshSyncContainerEntriesButtons();
        }

        private void RefreshUpdateLabel()
        {
            if (_updateLabel == null) return;

            _updateLabel.text = string.IsNullOrEmpty(_containerSo.LastUpdate)
                ? ""
                : "Last Synced at: " + _containerSo.LastUpdate;
        }

        private void RefreshSyncContainerEntriesButtons()
        {
            if (_containerSo.Project == null || _containerSo.ContainerInfo == null || _containerSo.ContainerInfo.id == 0)
            {
                _syncContainerButton?.SetEnabled(false);
                _hardSyncContainerButton?.SetEnabled(false);
                return;
            }

            if (_containerSo.Project.IsFetchingUpdate || _containerSo.IsUpdatingEntries)
            {
                _syncContainerButton?.SetEnabled(false);
                _hardSyncContainerButton?.SetEnabled(false);
                return;
            }

            _hardSyncContainerButton?.SetEnabled(true);
            _syncContainerButton?.SetEnabled(true);
        }

        private void SwitchProject(ChangeEvent<Object> evt)
        {
            ProjectSo newProject = evt.newValue as ProjectSo;
            ProjectSo oldProject = evt.previousValue as ProjectSo;
            
            _containerSo.ProjectChanged(newProject, oldProject);
            RefreshContainersDropdown();
            RefreshEntryFetchProgress();
            RefreshSyncContainerEntriesButtons();
            
            if (oldProject != null)
            {
                oldProject.OnBeginProjectUpdate -= RefreshSyncContainerEntriesButtons;
                oldProject.OnFinishProjectUpdate -= RefreshSyncContainerEntriesButtons;
            }
            
            if (newProject != null)
            {
                newProject.OnBeginProjectUpdate += RefreshSyncContainerEntriesButtons;
                newProject.OnFinishProjectUpdate += RefreshSyncContainerEntriesButtons;
            }
        }

        private void RefreshEntryFetchProgress()
        {
            if (_progressView == null)
                return;

            _progressView.Clear();

            if (_containerSo.UpdateContainerOperationRef != null)
            {
                ProgressBar progressBar = CreateUpdateContainerProgressBar(_containerSo.UpdateContainerOperationRef);
                progressBar.title = "Syncing Entries";
                _progressView.Add(progressBar);
            }
        }

        private ProgressBar CreateUpdateContainerProgressBar(UpdateContainerOperation updateContainerOperation)
        {
            ProgressBar progressBar = new ProgressBar
            {
                value = updateContainerOperation.Progress
            };
            progressBar.schedule.Execute(() =>
            {
                progressBar.value = updateContainerOperation.Progress;
                if (updateContainerOperation.IsFinished)
                    progressBar.title = progressBar.title + " " +
                                        (updateContainerOperation.IsFinishedSuccessfully ? "Done" : "Failed");
            }).Until(() => updateContainerOperation.IsFinished);

            return progressBar;
        }

        private void ProjectDataUpdate()
        {
            RefreshSyncContainerEntriesButtons();
            RefreshContainersDropdown();
        }

        private void ContainerEntriesUpdated(bool success)
        {
            RefreshSyncContainerEntriesButtons();

            if (!success) return;
            
            OpenEditorWindow();
        }

        private void OpenEditorWindow()
        {
            if (EditorWindow.HasOpenInstances<LocalizationTablesWindow>())
            {
                var editorWindow = EditorWindow.GetWindow(typeof(LocalizationTablesWindow));
                if(editorWindow)
                {
                    editorWindow.Close();
                    LocalizationTablesWindow.ShowWindow(_containerSo.StringTableCollection);
                }
                else
                {
                    LocalizationTablesWindow.ShowWindow(_containerSo.StringTableCollection);
                }
            }
            else
            {
                LocalizationTablesWindow.ShowWindow(_containerSo.StringTableCollection);
            }
        }
        
        #endregion

        #region Enable/Disable

        private void SetListeners()
        {
            _containerSo.OnFinishContainerEntriesUpdate += ContainerEntriesUpdated;
            _containerSo.OnProjectDataUpdated += ProjectDataUpdate;

            if (_containerSo.Project == null) return;

            _containerSo.Project.OnFinishProjectUpdate += RefreshSyncContainerEntriesButtons;
            _containerSo.Project.OnBeginProjectUpdate += RefreshSyncContainerEntriesButtons;
        }

        private void RemoveListeners()
        {
            _containerSo.OnFinishContainerEntriesUpdate -= ContainerEntriesUpdated;
            _containerSo.OnProjectDataUpdated -= ProjectDataUpdate;

            if (_containerSo.Project == null) return;

            _containerSo.Project.OnFinishProjectUpdate -= RefreshSyncContainerEntriesButtons;
            _containerSo.Project.OnBeginProjectUpdate -= RefreshSyncContainerEntriesButtons;
        }

        private void OnEnable()
        {
            _containerSo = (ContainerSo)target;
            SetListeners();

            if (!_containerSo.Project) return;

            long containerID = _containerSo.ContainerInfo?.id ?? 0;
            _containerSo.Project.AddContainerSo(_containerSo, containerID);
        }

        private void OnDisable()
        {
            RemoveListeners();
        }
        
        #endregion
    }
}