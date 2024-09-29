using System;
using System.Collections.Generic;
using Lungfetcher.Editor.Operations;
using Lungfetcher.Editor.Scriptables;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

public class ProjectElement : VisualElement
{
    private ProjectSo _projectSo;
    private const string XMLPath = "UI Documents/ProjectElement";
    private Button SyncProjectButton => this.Q<Button>("sync-project-btn");
    private Button SyncContainersButton => this.Q<Button>("sync-containers-btn");
    private Label UpdateLabel => this.Q<Label>("updated-label");
    private Label ProjectNameLabel => this.Q<Label>("project-name-label");
    private ScrollView LocalesView => this.Q<ScrollView>("locales-view");
    private ScrollView ProgressView => this.Q<ScrollView>("progress-view");
    private TextField APIKey => this.Q<TextField>("api-key-text");
    private Dictionary<ContainerSo, ProgressBar> _containerProgressBars = new Dictionary<ContainerSo, ProgressBar>();
    
    public event Action OnProjectUpdated; 
    
   //public new class UxmlFactory : UxmlFactory<ProjectElement> {}
    
    public ProjectElement(){}

    public ProjectElement(ProjectSo projectSo)
    {
        var asset = Resources.Load<VisualTreeAsset>(XMLPath);
        asset.CloneTree(this);
        
        SetupElement(projectSo);
    }
    
    private void SetupElement(ProjectSo projectSo)
    {
        if(!projectSo) return;
        _projectSo = projectSo;
        
        APIKey.BindProperty(new SerializedObject(_projectSo).FindProperty("apiKey"));
        
        SyncProjectButton.clicked += () =>
        {
            _projectSo.FetchUpdate();
            RefreshUpdateProgress();
            RefreshSyncButtons();
        };
        
        SyncContainersButton.clicked += () =>
        {
            _projectSo.SyncContainerSos();
            RefreshContainersProgress();
            RefreshSyncButtons();
        };
        
        ClearProgressBars();
        
        if (_projectSo.IsSyncingContainerSos())
            RefreshContainersProgress();
        if (_projectSo.IsFetchingUpdate)
            RefreshUpdateProgress();

        RefreshUpdateLabel();
        RefreshLocales();
        RefreshNameLabel();
        RefreshSyncButtons();
        
        SetListeners();
    }
    
    public void ReloadElement(ProjectSo projectSo)
    {
        Cleanup();
        SetupElement(projectSo);
    }

    private void RefreshNameLabel()
    {
        if(ProjectNameLabel == null) return;
        ProjectNameLabel.text = string.IsNullOrEmpty(_projectSo.ProjectInfo?.tag) ? "Not Synced" : 
            $"Tag: {_projectSo.ProjectInfo.tag}";
    }

    private void RefreshLocales()
    {
        LocalesView.Clear();
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
            LocalesView.Add(objectField);

            var serializedProperty = new SerializedObject(_projectSo).FindProperty("locales.Array.data[" + i + "].locale");

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

    private void RefreshUpdateLabel()
    {
        if(UpdateLabel == null) return;
        UpdateLabel.text = string.IsNullOrEmpty(_projectSo.LastUpdate)? "" : 
            $"Last Synced at: {_projectSo.LastUpdate}";
    }

    private void ClearProgressBars()
    {
        ProgressView?.Clear();
        _containerProgressBars = new Dictionary<ContainerSo, ProgressBar>();
    }

    private void RefreshContainersProgress()
    {
        if (!_projectSo.IsSyncingContainerSos())
            return;

        ClearProgressBars();

        foreach (var containerSo in _projectSo.UpdatingContainerSos)
        {
            UpdateContainersProgress(containerSo, containerSo.UpdateContainerOperationRef);
        }
    }

    private void UpdateContainersProgress(ContainerSo containerSo, RequestOperation updateContainerOperation)
    {
        if (_containerProgressBars.ContainsKey(containerSo)) return;

        var progressBar = CreateFetchProgressBar(updateContainerOperation);
        _containerProgressBars.Add(containerSo, progressBar);
        progressBar.title = "Syncing " + containerSo.name;
        ProgressView.Add(progressBar);
    }

    private ProgressBar CreateFetchProgressBar(RequestOperation requestOperation)
    {
        var progressBar = new ProgressBar
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

    private void RefreshSyncButtons()
    {
        if (_projectSo.IsFetchingUpdate || _projectSo.IsSyncingContainerSos())
        {
            SyncContainersButton?.SetEnabled(false);
            SyncProjectButton?.SetEnabled(false);
            return;
        }

        SyncProjectButton?.SetEnabled(true);
        SyncContainersButton?.SetEnabled(true);
    }

    private void RefreshUpdateProgress()
    {
        if (ProgressView == null || !_projectSo.IsFetchingUpdate)
            return;

        ClearProgressBars();

        if (_projectSo.UpdateProjectOperationRef == null) return;
        
        var progressBar = CreateFetchProgressBar(_projectSo.UpdateProjectOperationRef);
        progressBar.title = "Syncing Project Data";
        ProgressView.Add(progressBar);
    }
    
    private void ProjectUpdated()
    {
        RefreshSyncButtons();
        RefreshLocales();
        RefreshUpdateLabel();
        RefreshNameLabel();
        
        OnProjectUpdated?.Invoke();
    }

    public void Cleanup() => RemoveListeners();
    
    
    private void SetListeners()
    {
        _projectSo.OnFinishProjectUpdate += ProjectUpdated;
        _projectSo.OnAllContainerSyncFinished += RefreshSyncButtons;
        _projectSo.OnContainerSyncRequested += UpdateContainersProgress;
    }

    private void RemoveListeners()
    {
        _projectSo.OnFinishProjectUpdate -= ProjectUpdated;
        _projectSo.OnAllContainerSyncFinished -= RefreshSyncButtons;
        _projectSo.OnContainerSyncRequested -= UpdateContainersProgress;
    }
}
