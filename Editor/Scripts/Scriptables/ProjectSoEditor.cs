using System;
using System.Threading.Tasks;
using Lungfetcher.Editor;
using Lungfetcher.Editor.Scriptables;
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
    private VisualElement _root;
    

    public override VisualElement CreateInspectorGUI()
    {
        _projectSo = (ProjectSo)target;
        
        // Create a new VisualElement to be the root of our inspector UI
        _root = new VisualElement();
        
        // Load and clone a visual tree from UXML
        inspectorXML.CloneTree(_root);
        _syncProjectButton = _root.Q<Button>("sync-project");
        
        if (_syncProjectButton != null)
        {
            _syncProjectButton.clicked += () =>
            {
                _syncProjectButton.SetEnabled(false);
                var request = OperationsController.RequestProjectUpdate(_projectSo);
                request.OnFinished += RefreshSyncProjectButton;
            };
            
            RefreshSyncProjectButton();
        }

        // Return the finished inspector UI
        return _root;
    }

    private void OnDisable()
    {
        if (_syncProjectButton == null) return;

        if (_syncProjectButton.enabledSelf) return;
        
        var request = OperationsController.GetProjectUpdateRequest(_projectSo);

        if (request == null) return;
        
        request.OnFinished -= EnableSyncButton;
    }

    private void EnableSyncButton()
    {
        Debug.Log("button Enabled Fucker");
        _syncProjectButton?.SetEnabled(true);
    }

    private void RefreshSyncProjectButton()
    {
        if(_syncProjectButton == null)
            return;
        
        var request = OperationsController.GetProjectUpdateRequest(_projectSo);
        
        if (request is { IsFinished: false })
        {
            _syncProjectButton.SetEnabled(false);
            request.OnFinished += RefreshSyncProjectButton;
            return;
        }
        
        _syncProjectButton.SetEnabled(true);
    }
}
