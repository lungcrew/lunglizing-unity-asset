using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lungfetcher.Data;
using Lungfetcher.Editor.Scriptables;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(TableSo))]
public class TableSoEditor : Editor
{
    public VisualTreeAsset inspectorXML;
    private TableSo _tableSo;
    private List<Table> _projectTables;
    private List<string> _choices = new List<string>();
    private int _selected = 0;
    private VisualElement _root;
    private DropdownField _tableDropdown;

    public override VisualElement CreateInspectorGUI()
    {
        _tableSo = (TableSo)target;
        
        // Create a new VisualElement to be the root of our inspector UI
        _root = new VisualElement();
        
        // Load and clone a visual tree from UXML
        inspectorXML.CloneTree(_root);

        var projObjectField = _root.Q<ObjectField>("project-object");
        _tableDropdown = _root.Q<DropdownField>("tables-dropdown");
        
        projObjectField.RegisterValueChangedCallback(changeEvent =>
        {
            RefreshDropdown();
        });
        
        _tableDropdown.RegisterValueChangedCallback(changeEvent =>
        {
            DropdownChanged();
        });
        
        RefreshDropdown();
        
        // Return the finished inspector UI
        return _root;
    }

    private void RefreshDropdown()
    {
        if(_tableDropdown == null) return;
        
        if (_tableSo.Project)
        {
            if (_tableSo.Project.TableList.Count > 0)
            {
                _projectTables = _tableSo.Project.TableList;
                _choices = _projectTables.Select(x => x.name).ToList();
                _tableDropdown.choices = _choices.ToList();
                    
                if (_tableSo.TableInfo != null)
                {
                    var selectedTable = _projectTables.Find(x => x.id == _tableSo.TableInfo.id);
                        
                    if(selectedTable != null)
                        _selected = _projectTables.FindIndex(x => x.id == _tableSo.TableInfo.id);
                    else
                    {
                        _selected = -1;
                    }
                }
                else
                {
                    _selected = -1;
                }
                _tableDropdown.index = _selected;
            }
            else
            {
                _tableDropdown.choices.Clear();
            }
        }
        else
        {
            _tableDropdown.choices.Clear();
            _selected = -1;
            _tableDropdown.index = _selected;
        }
    }

    private void DropdownChanged()
    {
        if(_tableDropdown == null) return;

        _selected = _tableDropdown.index;
        if (_selected == -1) return;
        
        var selectedTable = _projectTables[_selected];
        
        _tableSo.UpdateTableInfo(selectedTable);
    }
}
