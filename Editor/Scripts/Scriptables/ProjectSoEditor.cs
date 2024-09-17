using System.Collections.Generic;
using Lungfetcher.Editor.Operations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace Lungfetcher.Editor.Scriptables
{
    [CustomEditor(typeof(ProjectSo))]
    public class ProjectSoEditor : UnityEditor.Editor
    {
        #region Fields

        public VisualTreeAsset inspectorXML;
        private ProjectSo _projectSo;
        private Button _syncProjectButton;
        private Button _syncTablesButton;
        private Label _updateLabel;
        private Label _projectNameLabel;
        private ScrollView _localesView;
        private ScrollView _progressView;
        private Dictionary<TableSo, ProgressBar> _tablesProgressBars = new Dictionary<TableSo, ProgressBar>();
        private VisualElement _root;
        
        #endregion

        #region Inspector
        
        public override VisualElement CreateInspectorGUI()
        {
            // Create a new VisualElement to be the root of our inspector UI
            _root = new VisualElement();

            // Load and clone a visual tree from UXML
            inspectorXML.CloneTree(_root);
            _syncProjectButton = _root.Q<Button>("sync-project-btn");
            _progressView = _root.Q<ScrollView>("progress-view");
            _syncTablesButton = _root.Q<Button>("sync-tables-btn");
            _localesView = _root.Q<ScrollView>("locales-view");
            _updateLabel = _root.Q<Label>("updated-label");
            _projectNameLabel = _root.Q<Label>("project-name-label");

            if (_syncProjectButton != null)
            {
                _syncProjectButton.clicked += () =>
                {
                    _projectSo.FetchUpdate();
                    RefreshUpdateProgress();
                    RefreshSyncButtons();
                };
            }

            if (_syncTablesButton != null)
            {
                _syncTablesButton.clicked += () =>
                {
                    _projectSo.SyncTableSos();
                    RefreshTablesProgress();
                    RefreshSyncButtons();
                };
            }

            ClearProgressBars();

            if (_projectSo.IsSyncingTableSos())
                RefreshTablesProgress();
            if (_projectSo.IsFetchingUpdate)
                RefreshUpdateProgress();

            RefreshUpdateLabel();
            RefreshLocales();
            RefreshNameLabel();
            RefreshSyncButtons();

            // Return the finished inspector UI
            return _root;
        }
        
        private void RefreshSyncButtons()
        {
            if (_projectSo.IsFetchingUpdate || _projectSo.IsSyncingTableSos())
            {
                _syncTablesButton?.SetEnabled(false);
                _syncProjectButton?.SetEnabled(false);
                return;
            }

            _syncProjectButton?.SetEnabled(true);
            _syncTablesButton?.SetEnabled(true);
        }

        private void ClearProgressBars()
        {
            _progressView?.Clear();
            _tablesProgressBars = new Dictionary<TableSo, ProgressBar>();
        }

        private void RefreshTablesProgress()
        {
            if (_progressView == null || !_projectSo.IsSyncingTableSos())
                return;

            ClearProgressBars();

            foreach (var table in _projectSo.UpdatingTableSos)
            {
                UpdateTablesProgress(table, table.UpdateTableOperationRef);
            }
        }

        private void UpdateTablesProgress(TableSo tableSo, RequestOperation updateTableOperation)
        {
            if (_tablesProgressBars.ContainsKey(tableSo)) return;

            var progressBar = CreateFetchProgressBar(updateTableOperation);
            _tablesProgressBars.Add(tableSo, progressBar);
            progressBar.title = "Syncing " + tableSo.name;
            _progressView.Add(progressBar);
        }

        private void RefreshUpdateProgress()
        {
            if (_progressView == null || !_projectSo.IsFetchingUpdate)
                return;

            ClearProgressBars();

            if (_projectSo.UpdateProjectOperationRef != null)
            {
                var progressBar = CreateFetchProgressBar(_projectSo.UpdateProjectOperationRef);
                progressBar.title = "Syncing Project Data";
                _progressView.Add(progressBar);
            }
        }

        private void RefreshNameLabel()
        {
            if(_projectNameLabel == null) return;
            _projectNameLabel.text = string.IsNullOrEmpty(_projectSo.ProjectInfo?.tag) ? "No Project Synced" : 
                $"Project Tag: {_projectSo.ProjectInfo.tag}";
        }

        private void RefreshUpdateLabel()
        {
            if(_updateLabel == null) return;
            _updateLabel.text = string.IsNullOrEmpty(_projectSo.LastUpdate)? "" : 
                $"Last Synced at: {_projectSo.LastUpdate}";
        }

        private void RefreshLocales()
        {
            if (_localesView == null)
                return;

            _localesView.Clear();
            if (_projectSo.ProjectLocales.Count <= 0)
                return;

            for (int i = 0; i < _projectSo.ProjectLocales.Count; i++)
            {
                var objectField = new ObjectField
                {
                    label = _projectSo.ProjectLocales[i].code,
                    objectType = typeof(Locale),
                    value = _projectSo.ProjectLocales[i].Locale
                };
                _localesView.Add(objectField);

                var serializedProperty = serializedObject.FindProperty("locales.Array.data[" + i + "].locale");

                if (serializedProperty != null)
                    objectField.BindProperty(serializedProperty);
                else
                {
                    var index = i;
                    objectField.RegisterValueChangedCallback(evt => 
                        _projectSo.ProjectLocales[index].SetLocale(evt.newValue as Locale));
                }
            }
        }

        private void ProjectUpdated()
        {
            RefreshSyncButtons();
            RefreshLocales();
            RefreshUpdateLabel();
            RefreshNameLabel();
        }

        private ProgressBar CreateFetchProgressBar(RequestOperation requestOperation)
        {
            ProgressBar progressBar = new ProgressBar
            {
                value = requestOperation.Progress
            };

            progressBar.schedule.Execute(() =>
            {
                progressBar.value = requestOperation.Progress;
                if (requestOperation.IsFinished)
                    progressBar.title = progressBar.title + " " +
                                        (requestOperation.IsFinishedSuccessfully ? "Done" : "Failed");
            }).Until(() => requestOperation.IsFinished);

            return progressBar;
        }
        
        #endregion
        
        #region Enable/Disable
        
        private void SetListeners()
        {
            _projectSo.OnFinishProjectUpdate += ProjectUpdated;
            _projectSo.OnAllTableSyncFinished += RefreshSyncButtons;
            _projectSo.OnTableSyncRequested += UpdateTablesProgress;
        }
        
        private void RemoveListeners()
        {
            _projectSo.OnFinishProjectUpdate -= ProjectUpdated;
            _projectSo.OnAllTableSyncFinished -= RefreshSyncButtons;
            _projectSo.OnTableSyncRequested -= UpdateTablesProgress;
        }
        
        private void OnEnable()
        {
            _projectSo = (ProjectSo)target;
            SetListeners();
        }

        private void OnDisable()
        {
            RemoveListeners();
        }
        
        #endregion
    }
}
