using System.Collections.Generic;
using System.Linq;
using Lungfetcher.Data;
using Lungfetcher.Editor;
using Lungfetcher.Editor.Scriptables;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

[CustomEditor(typeof(TableSo))]
public class TableSoEditor : Editor
{
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

        if(_syncTableButton != null)
        {
            _syncTableButton.clicked += () =>
            {
                _tableSo.FetchEntries();
                RefreshEntryFetchProgress();
                RefreshSyncTableButton();
            };
        }

        _projectObject.schedule.Execute(() => _projectObject.RegisterValueChangedCallback(SwitchProject));
        
        RefreshDropdown();
        RefreshLocaleFields();
        RefreshUpdateLabel();
        RefreshSyncTableButton();
        RefreshEntryFetchProgress();
        
        // Return the finished inspector UI
        return _root;
    }

    private void RefreshDropdown()
    {
        if(_tableDropdown == null || _tableSo == null) return;
        
        _tableDropdown.UnregisterValueChangedCallback(DropdownChanged);
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
                        _tableDropdown.index = _choices.IndexOf(selectedTable.name);
                    }
                    else
                    {
                        _tableSo.UpdateTableInfo(null);
                        _tableDropdown.index = -1;
                    }
                }
                else
                {
                    _tableDropdown.index = -1;
                }
            }
            else
            {
                _tableSo.UpdateTableInfo(null);
                _tableDropdown.choices.Clear();
            }
        }
        else
        {
            _tableSo.UpdateTableInfo(null);
            _tableDropdown.choices.Clear();
            _tableDropdown.index = -1;
        }
        
        _tableDropdown.schedule.Execute(() => _tableDropdown.RegisterValueChangedCallback(DropdownChanged));
    }

    private void DropdownChanged(ChangeEvent<string> evt)
    {
        if(_tableDropdown == null) return;
        
        if (_tableDropdown.index == -1) return;
        
        var selectedTable = _projectTables[_tableDropdown.index];
        
        _tableSo.UpdateTableInfo(selectedTable);
        RefreshSyncTableButton();
    }

    private void RefreshLocaleFields()
    {
        if(_localesView == null) return;
        
        _localesView.Clear();

        for (int i = 0; i < _tableSo.Locales.Count; i++)
        {
            var objectField = new ObjectField();
            objectField.label = _tableSo.Locales[i].code;
            objectField.objectType = typeof(Locale);
            _localesView.Add(objectField);
            objectField.value = _tableSo.Locales[i].Locale;
            
            var serializedProperty = serializedObject.FindProperty("locales.Array.data["+i+"].locale");
            
            if(serializedProperty != null)
                objectField.BindProperty(serializedProperty);
        }
    }

    private void SyncTable()
    {
        if (_tableSo.Project == null || _tableSo.TableInfo == null || _tableSo.TableInfo.id == 0)
        {
            return;
        }
        if (_tableSo.Project.IsFetchingUpdate || _tableSo.IsUpdatingEntries)
        {
            return;
        }
    }

    private void RefreshUpdateLabel()
    {
        if(_updateLabel == null) return;
        
        _updateLabel.text = string.IsNullOrEmpty(_tableSo.LastUpdate) ? "" : "Last Synced at: " + _tableSo.LastUpdate ;
    }

    private void RefreshSyncTableButton()
    {
        if(_syncTableButton == null) return;
        
        if (_tableSo.Project == null || _tableSo.TableInfo == null || _tableSo.TableInfo.id == 0)
        {
            _syncTableButton.SetEnabled(false);
            return;
        }
        
        if (_tableSo.Project.IsFetchingUpdate || _tableSo.IsUpdatingEntries)
        {
            _syncTableButton.SetEnabled(false);
            return;
        }
        
        _syncTableButton.SetEnabled(true);
    }

    private void SwitchProject(ChangeEvent<Object> evt)
    {
        ProjectSo newProject = evt.newValue as ProjectSo;
        ProjectSo oldProject = evt.previousValue as ProjectSo;
        _tableSo.ProjectChanged(newProject, oldProject);
        RefreshDropdown();
        _tableSo.UpdateLocales();
        RefreshLocaleFields();
        RefreshEntryFetchProgress();
        RefreshSyncTableButton();
        
        if(oldProject != null)
        {
            oldProject.OnBeginFetchUpdate -= RefreshSyncTableButton;
            oldProject.OnFinishFetchUpdate -= RefreshSyncTableButton;
        }
        if(newProject != null)
        {
            newProject.OnBeginFetchUpdate += RefreshSyncTableButton;
            newProject.OnFinishFetchUpdate += RefreshSyncTableButton;
        }
    }

    private void RefreshEntryFetchProgress()
    {
        if(_progressView == null) 
            return;
        
        _progressView.Clear();

        if (_tableSo.FetchEntriesOperation != null)
        {
            ProgressBar progressBar = CreateFetchProgressBar(_tableSo.FetchEntriesOperation);
            progressBar.title = "Syncing Entries";
            _progressView.Add(progressBar);
        }
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
            {
                progressBar.title = progressBar.title + " " + (requestOperation.IsFinishedSuccessfully ? "Done" : "Failed");
            }
        }).Until(() => requestOperation.IsFinished);

        return progressBar;
    }
    
    private void SetListeners()
    {
        _tableSo.OnFinishEntryUpdate += RefreshSyncTableButton;

        if (_tableSo.Project == null) return;
        
        _tableSo.Project.OnFinishFetchUpdate += RefreshSyncTableButton;
        _tableSo.Project.OnBeginFetchUpdate += RefreshSyncTableButton;
        _tableSo.OnFinishEntryUpdate += RefreshSyncTableButton;
    }

    private void RemoveListeners()
    {
        _tableSo.OnFinishEntryUpdate -= RefreshSyncTableButton;

        if (_tableSo.Project == null) return;
        
        _tableSo.Project.OnFinishFetchUpdate -= RefreshSyncTableButton;
        _tableSo.Project.OnBeginFetchUpdate -= RefreshSyncTableButton;
    }

    private void OnEnable()
    {
        _tableSo = (TableSo)target;
        SetListeners();

        if (!_tableSo.Project) return;
        
        if (_tableSo.TableInfo.id != 0)
            _tableSo.Project.AddTableSo(_tableSo, _tableSo.TableInfo.id);
    }

    private void OnDisable()
    {
        RemoveListeners();
    }
}
