using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lungfetcher.Data;
using Lungfetcher.Editor;
using Lungfetcher.Editor.Scriptables;
using Lungfetcher.Helper;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(ProjectSo))]
public class ProjectSoEditor : Editor
{
    
    public VisualTreeAsset inspectorXML;
    private ProjectSo _projectSo;
    private Button _syncProjectButton;
    private Button _syncTablesButton;
    private ScrollView _progressView;
    private Dictionary<string, ProgressBar> _tablesProgressBars = new Dictionary<string, ProgressBar>();
    private VisualElement _root;

    public override VisualElement CreateInspectorGUI()
    {
        // Create a new VisualElement to be the root of our inspector UI
        _root = new VisualElement();
        
        // Load and clone a visual tree from UXML
        inspectorXML.CloneTree(_root);
        _syncProjectButton = _root.Q<Button>("sync-project-btn");
        _progressView = _root.Q<ScrollView>("progress-view");
        _syncTablesButton = _root.Q<Button>("sync-tables-btn");
        
        if(_syncProjectButton != null)
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
                _projectSo.SyncTables();
                RefreshTablesProgress();
                RefreshSyncButtons();
            };
        }

        ClearProgressBars();
        
        if(_projectSo.IsFetchingTables())
            RefreshTablesProgress();
        if(_projectSo.IsFetchingUpdate)
            RefreshUpdateProgress();
        
        RefreshSyncButtons();
        
        // Return the finished inspector UI
        return _root;
    }

    private void OnEnable()
    {
        _projectSo = (ProjectSo)target;
        _projectSo.OnFinishFetchUpdate += RefreshSyncButtons;
        _projectSo.OnAllTableSyncFinished += RefreshSyncButtons;
        _projectSo.OnTableSyncRequested += UpdateTablesProgress;
    }

    private void OnDisable()
    {
        _projectSo.OnFinishFetchUpdate -= RefreshSyncButtons;
        _projectSo.OnAllTableSyncFinished -= RefreshSyncButtons;
        _projectSo.OnTableSyncRequested -= UpdateTablesProgress;
    }
    

    private void RefreshSyncButtons()
    {
        if(_projectSo.IsFetchingUpdate || _projectSo.IsFetchingTables())
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
        _tablesProgressBars = new Dictionary<string, ProgressBar>();
    }

    private void RefreshTablesProgress()
    {
        if(_progressView == null || !_projectSo.IsFetchingTables()) 
            return;

        ClearProgressBars();

        foreach (var table in _projectSo.FetchingTablesDic.Keys)
        {
            UpdateTablesProgress(table, _projectSo.FetchingTablesDic[table]);
        }
    }

    private void UpdateTablesProgress(string tableName, RequestOperation requestOperation)
    {
        if(_tablesProgressBars.ContainsKey(tableName)) return;
        
        var progressBar = CreateFetchProgressBar(requestOperation);
        _tablesProgressBars.Add(tableName, progressBar);
        progressBar.title = "Syncing " + tableName;
        _progressView.Add(progressBar);
    }

    private void RefreshUpdateProgress()
    {
        if(_progressView == null || !_projectSo.IsFetchingUpdate) 
            return;

        ClearProgressBars();
        
        if (_projectSo.FetchProjectInfoOperation != null)
        {
            var progressBar = CreateFetchProgressBar(_projectSo.FetchProjectInfoOperation);
            progressBar.title = "Syncing Project Info";
            _progressView.Add(progressBar);
        }
        
        if (_projectSo.FetchTableListOperation != null)
        {
            var progressBar = CreateFetchProgressBar(_projectSo.FetchTableListOperation);
            progressBar.title = "Syncing Tables Info";
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
}
