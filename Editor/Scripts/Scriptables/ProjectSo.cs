using System.Collections.Generic;
using System.Linq;
using Lungfetcher.Data;
using Lungfetcher.Helper;
using UnityEditor;
using UnityEngine;


namespace Lungfetcher.Editor.Scriptables
{
    [CreateAssetMenu(fileName = "ProjectLungfetcher", menuName = "Lungfetcher/ProjectScriptable", order = 1)]
    public class ProjectSo : ScriptableObject
    {

        [SerializeField]
        private string apiKey = "QcS74OK.M4L77werD9BhsrGGPjixUgKwjVmFrfXQ";
        [SerializeField]
        private LongScriptableObjectDictionary tableDic;
        [SerializeField]
        private Project projectInfo;
        [SerializeField]
        private List<Table> tableList;
        
        public string ApiKey => apiKey;
        public Project ProjectInfo => projectInfo;
        public List<Table> TableList => tableList;

        public string TablesPath { get; private set; }

        public void SyncTables(List<Table> tables)
        {
            tableList = tables;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
        
        public void SetProject(Project project)
        {
            SyncProjectInfo(project);
            TablesPath = $"{AssetDatabase.GetAssetPath(this)}/Tables/";
        }

        public void SyncProjectInfo(Project project)
        {
            projectInfo = project;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        public void UpdateAllTables(List<Table> tables)
        {
            if (tableDic.Count <= 0)
            {
                foreach (Table table in tables)
                {
                    tableDic.Add(table.id, CreateTable(table));
                }
            }
            else
            {
                foreach (Table table in tables)
                {
                    if (tableDic.TryGetValue(table.id, out var scriptableObject))
                    {
                        if (scriptableObject == null)
                        {
                            tableDic[table.id] = CreateTable(table);
                        }
                        else
                        {
                            TableSo tableSo = (TableSo)scriptableObject;
                            tableSo.UpdateTableInfo(table);
                        }
                    }
                    else
                    {
                        tableDic.Add(table.id, CreateTable(table));
                    }
                }
            }
        }

        public void UpdateAllTableEntries(long tableId, List<Entry> entries, bool removeUnused = false)
        {
            if (tableDic.TryGetValue(tableId, out var scriptableObject))
            {
                if (!scriptableObject)
                {
                    DebugLog.TableReferenceMissing(tableId, ProjectInfo.id);
                    return;
                }
                TableSo tableSo = (TableSo)scriptableObject;
                tableSo.UpdateEntries(entries, removeUnused);
            }
            else
            {
                DebugLog.TableNotFound(tableId, ProjectInfo.id);
            }
        }
        
        public void UpdateTableEntry(long tableId, Entry entry)
        {
            if (tableDic.TryGetValue(tableId, out var scriptableObject))
            {
                if (!scriptableObject)
                {
                    DebugLog.TableReferenceMissing(tableId, ProjectInfo.id);
                    return;
                }
                TableSo tableSo = (TableSo)scriptableObject;
                tableSo.UpdateEntry(entry);
            }
            else
            {
                DebugLog.TableNotFound(tableId, ProjectInfo.id);
            }
        }
        
        public TableSo CreateTable(Table table)
        {
            TableSo tableSo = CreateInstance<TableSo>();
            AssetDatabase.CreateAsset(tableSo, $"{TablesPath}/{table.name}.asset");
            tableSo.SetupTable(table);
            return tableSo;
        }
        
        public void UpdateTable(Table table)
        {
            if (tableDic.TryGetValue(table.id, out var value))
            {
                if (value == null)
                {
                    tableDic.Add(table.id, CreateTable(table));
                }
                else
                {
                    TableSo tableSo = (TableSo)value;
                    tableSo.UpdateTableInfo(table);
                }
            }
            else
            {
                tableDic.Add(table.id, CreateTable(table));
            }
        }

        public void RemoveAllTables()
        {
            List<KeyValuePair<long, ScriptableObject>> tableDicTempList = tableDic.ToList();
            
            foreach (var valuePair in tableDicTempList.Where(tableValuePair 
                         => tableValuePair.Value))
            {
                RemoveTable(valuePair.Key);
            }
            tableDic.Clear();
        }

        public void RemoveUnusedTables(List<Table> tables)
        {
            List<KeyValuePair<long, ScriptableObject>> tableDicTempList = tableDic.ToList();
            
            foreach (var valuePair in tableDicTempList.Where(tableValuePair =>
                         tables.Find(x => x.id == tableValuePair.Key) == null))
            {
               RemoveTable(valuePair.Key);
            }
        }

        public void RemoveTable(long tableId)
        {
            tableDic.Remove(tableId, out ScriptableObject value);
            if (!value) return;
            string path = AssetDatabase.GetAssetPath(value);
            AssetDatabase.DeleteAsset(path);
        }

        public TableSo GetTable(long tableId)
        {
            if (tableDic.TryGetValue(tableId, out var scriptableObject))
            {
                if (scriptableObject) return (TableSo)scriptableObject;
                
                DebugLog.TableReferenceMissing(tableId, ProjectInfo.id);
                return null;
            }

            DebugLog.TableNotFound(tableId, ProjectInfo.id);
            return null;
        }

        public Entry GetEntryFromTable(long tableId, long entryId)
        {
            if (tableDic.TryGetValue(tableId, out var scriptableObject))
            {
                if (!scriptableObject)
                {
                    DebugLog.TableReferenceMissing(tableId, ProjectInfo.id);
                    return null;
                }
                
                TableSo tableSo = (TableSo)scriptableObject;
                
                return tableSo.GetEntry(entryId);
            }
            
            DebugLog.TableNotFound(tableId, ProjectInfo.id);
            return null;
        }

        public string GetProjectPath()
        {
            return AssetDatabase.GetAssetPath(this);
        }
        
        
    }
}