using System;
using System.Collections.Generic;
using Lungfetcher.Editor.Scriptables;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class LungFetcherWindow : EditorWindow
{
    private VisualTreeAsset _visualTreeAsset;
    private List<ProjectSo> _projectSoList = new List<ProjectSo>();
    private DropdownField _projectsDropdown;
    private ProjectElement _projectElement;
    private ScrollView _projectRoot;
    private const string TreeAssetPath = "UI Documents/LungFetcherWindow";

    [MenuItem("Window/UI Toolkit/LungFetcherWindow")]
    public static void ShowExample()
    {
        LungFetcherWindow wnd = GetWindow<LungFetcherWindow>();
        wnd.titleContent = new GUIContent("LungFetcherWindow");
    }
    
    public void CreateGUI()
    {
        _visualTreeAsset = Resources.Load<VisualTreeAsset>(TreeAssetPath);
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        
        SetProjectsList();

        // Instantiate UXML
        VisualElement uxml = _visualTreeAsset.Instantiate();
        root.Add(uxml);

        _projectsDropdown = root.Q<DropdownField>("projects-dropdown");
        _projectRoot = root.Q<ScrollView>("left-view");
        
        FillProjectsDropdown();
        
        _projectsDropdown.RegisterValueChangedCallback(ProjectDropdownValueChanged);
    }

    public void OnDestroy()
    {
        _projectElement?.Cleanup();
    }

    private void SetProjectsList()
    {
        var allProjectSoGuids = AssetDatabase.FindAssets("t:ProjectSo");
        _projectSoList = new List<ProjectSo>();
        foreach (var guid in allProjectSoGuids)
        {
            var projectSoAsset = AssetDatabase.LoadAssetAtPath<ProjectSo>(AssetDatabase.GUIDToAssetPath(guid));
            _projectSoList.Add(projectSoAsset);
        }
        if(_projectSoList.Count >= 2)
            _projectSoList.Sort((x, y) => 
                string.CompareOrdinal(string.IsNullOrEmpty(x.ProjectInfo.tag) ? x.name : x.ProjectInfo.tag, 
                    string.IsNullOrEmpty(y.ProjectInfo.tag) ? y.name : y.ProjectInfo.tag));
    }

    private void FillProjectsDropdown()
    {
        if(_projectsDropdown == null) return;
        
        _projectsDropdown.choices.Clear();

        for (int i = 0; i < _projectSoList.Count; i++)
        {
            var projectSo = _projectSoList[i];
            var stringEntry = string.IsNullOrEmpty(projectSo.ProjectInfo.tag) ? 
                projectSo.name : projectSo.ProjectInfo.tag;
            _projectsDropdown.choices.Add(stringEntry);
        }
    }
    
    private void ProjectDropdownValueChanged(ChangeEvent<string> evt)
    {
        if (_projectElement != null)
            _projectRoot.Remove(_projectElement);
        if (_projectsDropdown.index == -1)
        {
            _projectElement?.Cleanup();
        }
        else
        {
            var projectSo = _projectSoList[_projectsDropdown.index];
            if (!projectSo)
            {
                _projectSoList.RemoveAt(_projectsDropdown.index);
                _projectsDropdown.choices.RemoveAt(_projectsDropdown.index);
                _projectsDropdown.SetValueWithoutNotify("");
                return;
            }
            if(_projectElement == null)
                _projectElement = new ProjectElement(_projectSoList[_projectsDropdown.index]);
            else
            {
                _projectElement.ReloadElement(_projectSoList[_projectsDropdown.index]);
            }
            _projectRoot.Add(_projectElement);
        }
    }
    
}
