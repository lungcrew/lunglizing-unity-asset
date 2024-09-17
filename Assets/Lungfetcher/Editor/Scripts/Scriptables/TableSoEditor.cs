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
    [CustomEditor(typeof(TableSo))]
    public class TableSoEditor : UnityEditor.Editor
    {
        #region Fields
        
        public VisualTreeAsset inspectorXML;
        private TableSo _tableSo;
        private List<Table> _projectTables;
        private List<string> _choices = new List<string>();
        private Label _updateLabel;
        private VisualElement _root;
        private DropdownField _tableDropdown;
        private ScrollView _localesView;
        private ScrollView _progressView;
        private ObjectField _projectObject;
        private Button _syncTableButton;
        private Button _hardSyncTableButton;
        
        #endregion

        #region Inspector

        public override VisualElement CreateInspectorGUI()
        {
            // Create a new VisualElement to be the root of our inspector UI
            _root = new VisualElement();

            // Load and clone a visual tree from UXML
            inspectorXML.CloneTree(_root);

            _tableDropdown = _root.Q<DropdownField>("tables-dropdown");
            _updateLabel = _root.Q<Label>("updated-label");
            _localesView = _root.Q<ScrollView>("locales-view");
            _progressView = _root.Q<ScrollView>("progress-view");
            _projectObject = _root.Q<ObjectField>("project-object");
            _syncTableButton = _root.Q<Button>("sync-table-btn");
            _hardSyncTableButton = _root.Q<Button>("hard-sync-table-btn");

            if (_syncTableButton != null)
                _syncTableButton.clicked += () => SyncTable();
            
            if(_hardSyncTableButton != null)
                _hardSyncTableButton.clicked += () => SyncTable(true);
            
            _projectObject.schedule.Execute(() => _projectObject.RegisterValueChangedCallback(SwitchProject));
            
            _tableDropdown.RegisterValueChangedCallback(DropdownChanged);

            RefreshTablesDropdown();
            RefreshUpdateLabel();
            RefreshSyncTableEntriesButtons();
            RefreshEntryFetchProgress();

            // Return the finished inspector UI
            return _root;
        }

        private void SyncTable(bool hardSync = false)
        {
            _tableSo.FetchEntries(hardSync);
            RefreshEntryFetchProgress();
            RefreshSyncTableEntriesButtons();
        }

        private void RefreshTablesDropdown()
        {
            if (_tableDropdown == null || _tableSo == null) return;
            
            if (_tableSo.Project)
            {
                if (_tableSo.Project.TableList.Count > 0)
                {
                    _projectTables = _tableSo.Project.TableList;
                    _choices = _projectTables.Select(x => x.name).ToList();
                    _tableDropdown.choices = _choices;

                    if (_tableSo.TableInfo != null)
                    {
                        var selectedTable = _projectTables.Find(x => x.id == _tableSo.TableInfo.id);
                        if (selectedTable != null)
                        {
                            _tableDropdown.SetValueWithoutNotify(selectedTable.name);
                        }
                        else
                        {
                            _tableSo.ChangeTableInfo(null);
                            _tableDropdown.SetValueWithoutNotify(null);
                        }
                    }
                    else
                    {
                        _tableDropdown.SetValueWithoutNotify(null);
                    }
                }
                else
                {
                    _tableSo.ChangeTableInfo(null);
                    _tableDropdown.choices.Clear();
                    _tableDropdown.SetValueWithoutNotify(null);
                }
            }
            else
            {
                _tableSo.ChangeTableInfo(null);
                _tableDropdown.choices.Clear();
                _tableDropdown.SetValueWithoutNotify(null);
            }
        }

        private void DropdownChanged(ChangeEvent<string> evt)
        {
            if (_tableDropdown == null) return;

            if (_tableDropdown.index == -1 || _tableDropdown.value == null) return;

            var selectedTable = _projectTables[_tableDropdown.index];

            _tableSo.ChangeTableInfo(selectedTable);
            RefreshSyncTableEntriesButtons();
        }

        private void RefreshUpdateLabel()
        {
            if (_updateLabel == null) return;

            _updateLabel.text = string.IsNullOrEmpty(_tableSo.LastUpdate)
                ? ""
                : "Last Synced at: " + _tableSo.LastUpdate;
        }

        private void RefreshSyncTableEntriesButtons()
        {
            if (_tableSo.Project == null || _tableSo.TableInfo == null || _tableSo.TableInfo.id == 0)
            {
                _syncTableButton?.SetEnabled(false);
                _hardSyncTableButton?.SetEnabled(false);
                return;
            }

            if (_tableSo.Project.IsFetchingUpdate || _tableSo.IsUpdatingEntries)
            {
                _syncTableButton?.SetEnabled(false);
                _hardSyncTableButton?.SetEnabled(false);
                return;
            }

            _hardSyncTableButton?.SetEnabled(true);
            _syncTableButton?.SetEnabled(true);
        }

        private void SwitchProject(ChangeEvent<Object> evt)
        {
            ProjectSo newProject = evt.newValue as ProjectSo;
            ProjectSo oldProject = evt.previousValue as ProjectSo;
            
            _tableSo.ProjectChanged(newProject, oldProject);
            RefreshTablesDropdown();
            RefreshEntryFetchProgress();
            RefreshSyncTableEntriesButtons();
            
            if (oldProject != null)
            {
                oldProject.OnBeginProjectUpdate -= RefreshSyncTableEntriesButtons;
                oldProject.OnFinishProjectUpdate -= RefreshSyncTableEntriesButtons;
            }
            
            if (newProject != null)
            {
                newProject.OnBeginProjectUpdate += RefreshSyncTableEntriesButtons;
                newProject.OnFinishProjectUpdate += RefreshSyncTableEntriesButtons;
            }
        }

        private void RefreshEntryFetchProgress()
        {
            if (_progressView == null)
                return;

            _progressView.Clear();

            if (_tableSo.UpdateTableOperationRef != null)
            {
                ProgressBar progressBar = CreateUpdateTableProgressBar(_tableSo.UpdateTableOperationRef);
                progressBar.title = "Syncing Entries";
                _progressView.Add(progressBar);
            }
        }

        private ProgressBar CreateUpdateTableProgressBar(UpdateTableOperation updateTableOperation)
        {
            ProgressBar progressBar = new ProgressBar
            {
                value = updateTableOperation.Progress
            };
            progressBar.schedule.Execute(() =>
            {
                progressBar.value = updateTableOperation.Progress;
                if (updateTableOperation.IsFinished)
                    progressBar.title = progressBar.title + " " +
                                        (updateTableOperation.IsFinishedSuccessfully ? "Done" : "Failed");
            }).Until(() => updateTableOperation.IsFinished);

            return progressBar;
        }

        private void ProjectDataUpdate()
        {
            RefreshSyncTableEntriesButtons();
            RefreshTablesDropdown();
        }

        private void TableEntriesUpdated(bool success)
        {
            RefreshSyncTableEntriesButtons();

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
                    LocalizationTablesWindow.ShowWindow(_tableSo.StringTableCollection);
                }
                else
                {
                    LocalizationTablesWindow.ShowWindow(_tableSo.StringTableCollection);
                }
            }
            else
            {
                LocalizationTablesWindow.ShowWindow(_tableSo.StringTableCollection);
            }
        }
        
        #endregion

        #region Enable/Disable

        private void SetListeners()
        {
            _tableSo.OnFinishTableEntriesUpdate += TableEntriesUpdated;
            _tableSo.OnProjectDataUpdated += ProjectDataUpdate;

            if (_tableSo.Project == null) return;

            _tableSo.Project.OnFinishProjectUpdate += RefreshSyncTableEntriesButtons;
            _tableSo.Project.OnBeginProjectUpdate += RefreshSyncTableEntriesButtons;
        }

        private void RemoveListeners()
        {
            _tableSo.OnFinishTableEntriesUpdate -= TableEntriesUpdated;
            _tableSo.OnProjectDataUpdated -= ProjectDataUpdate;

            if (_tableSo.Project == null) return;

            _tableSo.Project.OnFinishProjectUpdate -= RefreshSyncTableEntriesButtons;
            _tableSo.Project.OnBeginProjectUpdate -= RefreshSyncTableEntriesButtons;
        }

        private void OnEnable()
        {
            _tableSo = (TableSo)target;
            SetListeners();

            if (!_tableSo.Project) return;

            long tableId = _tableSo.TableInfo?.id ?? 0;
            _tableSo.Project.AddTableSo(_tableSo, tableId);
        }

        private void OnDisable()
        {
            RemoveListeners();
        }
        
        #endregion
    }
}