using System.Collections.Generic;
using System.Linq;
using Lungfetcher.Data;
using Lungfetcher.Editor.Helper;
using Lungfetcher.Helper;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;


namespace Lungfetcher.Editor.Scriptables
{
    [CreateAssetMenu(fileName = "ProjectLungfetcher", menuName = "Lungfetcher/ProjectScriptable", order = 1)]
    public class ProjectSo : ScriptableObject
    {
        [SerializeField]
        private string apiKey = "QcS74OK.M4L77werD9BhsrGGPjixUgKwjVmFrfXQ";
        [SerializeField]
        private LongTablesSoDictionary projectTableSoDic;
        [SerializeField]
        private Project projectInfo;
        [SerializeField]
        private List<Table> tableList;
        
        private Dictionary<string, RequestOperation> _fetchingTablesDic = new Dictionary<string, RequestOperation>();

        public string ApiKey => apiKey;
        public Project ProjectInfo => projectInfo;
        public List<Table> TableList => tableList;
        public FetchOperation<Project> FetchProjectInfoOperation { get; private set; }
        public FetchOperation<List<Table>> FetchTableListOperation { get; private set; }
        public LongTablesSoDictionary ProjectTableSoDic => projectTableSoDic;
        public Dictionary<string, RequestOperation> FetchingTablesDic => _fetchingTablesDic;

        public string TablesPath { get; private set; }
        public bool IsFetchingUpdate { get; private set; } = false;

        public event UnityAction OnBeginFetchUpdate;
        public event UnityAction OnFinishFetchUpdate;
        public event UnityAction<string, RequestOperation> OnTableSyncRequested;
        public event UnityAction OnAllTableSyncFinished;

        public bool IsFetchingTables()
        {
            return _fetchingTablesDic.Count > 0;
        }

        public void SyncTableInfo(List<Table> tables)
        {
            tableList = tables;
            RemoveUnusedTablesFromDict();
            
            EditorUtility.SetDirty(this);
        }

        private void RemoveUnusedTablesFromDict()
        {
            if(tableList.Count <= 0) return;
            
            foreach (long tableId in projectTableSoDic.Keys)
            {
                var tableFound = tableList.Find(table => tableId == table.id);
                if (tableFound == null)
                {
                    projectTableSoDic.Remove(tableId);
                }
            }
        }

        public void SyncProjectInfo(Project project, bool save = true)
        {
            projectInfo = project;
            if(!save) return;
            
            EditorUtility.SetDirty(this);
        }
        

        private void UpdateFetchStatus()
        {
            if (FetchProjectInfoOperation is { IsFinished: false })
            {
                if (IsFetchingUpdate) return;
                IsFetchingUpdate = true; 
                OnBeginFetchUpdate?.Invoke();
                return;
            }

            if (FetchTableListOperation is { IsFinished: false })
            {
                if (IsFetchingUpdate) return;
                IsFetchingUpdate = true; 
                OnBeginFetchUpdate?.Invoke();
                return;
            }

            if (IsFetchingUpdate)
            {
                IsFetchingUpdate = false;
                OnFinishFetchUpdate?.Invoke();
            }
        }

        public void FetchUpdate()
        {
            if(IsFetchingUpdate) return;

            FetchProjectInfo();
            FetchTables();
        }

        public void FetchProjectInfo()
        {
            ReceiveProjectInfoFetch();
            FetchProjectInfoOperation = OperationsController.RequestFetchProjectInfo("info", apiKey);
            UpdateFetchStatus();
            FetchProjectInfoOperation.OnFinished += ReceiveProjectInfoFetch;
        }

        public void FetchTables()
        {
            ReceiveTablesFetch();
            FetchTableListOperation = OperationsController.RequestFetchProjectTables("tables", apiKey);
            UpdateFetchStatus();
            FetchTableListOperation.OnFinished += ReceiveTablesFetch;
        }

        private void ReceiveProjectInfoFetch()
        {
            if(FetchProjectInfoOperation == null) return;
            
            if(!FetchProjectInfoOperation.IsFinished) return;

            if (FetchProjectInfoOperation.IsFinishedSuccessfully)
            {
                if(FetchProjectInfoOperation.ResponseData != null)
                    SyncProjectInfo(FetchProjectInfoOperation.ResponseData);
            }
            else
            {
                DebugErrors.ProjectInfoFailed(FetchProjectInfoOperation.AccessKey);
            }
            
            FetchProjectInfoOperation.OnFinished -= ReceiveProjectInfoFetch;
            FetchProjectInfoOperation = null;
            
            UpdateFetchStatus();
        }

        private void ReceiveTablesFetch()
        {
            if (FetchTableListOperation == null) return;

            if (!FetchTableListOperation.IsFinished) return;

            if (FetchTableListOperation.IsFinishedSuccessfully)
            {
                if (FetchTableListOperation.ResponseData != null)
                    SyncTableInfo(FetchTableListOperation.ResponseData);
            }
            else
            {
                DebugErrors.ProjectFetchTablesFailed(FetchTableListOperation.AccessKey);
            }

            FetchTableListOperation.OnFinished -= ReceiveTablesFetch;
            FetchTableListOperation = null;

            UpdateFetchStatus();
        }

        public void AddTableSo(TableSo tableSo, long tableId)
        {
            if (projectInfo == null || tableList == null)
            {
                DebugErrors.ProjectMissingInfo();
                return;
            }
            
            if (!projectTableSoDic.TryGetValue(tableId, out var tableSoList))
            {
                tableSoList = new TableSoList
                {
                    tableSos = new List<TableSo> { tableSo }
                };
                projectTableSoDic.Add(tableId, tableSoList);
            }
            else
            {
                if (tableSoList.tableSos.Contains(tableSo))
                {
                    return;
                }

                tableSoList.tableSos.Add(tableSo);
            }

            Debug.Log("Added " + tableId);
            EditorUtility.SetDirty(this);
        }

        public void SwitchTableSoId(TableSo tableSo, long oldTableId, long newTableId)
        {
            if (projectInfo == null || tableList == null)
            {
                DebugErrors.ProjectMissingInfo();
                return;
            }
            Debug.Log("Switching");
            RemoveTableSo(tableSo, oldTableId);
            AddTableSo(tableSo, newTableId);
        }

        public void RemoveTableSo(TableSo tableSo, long tableId)
        {
            if (projectInfo == null || tableList == null)
            {
                DebugErrors.ProjectMissingInfo();
                return;
            }
            Debug.Log("Removing " + tableId);

            if (!projectTableSoDic.TryGetValue(tableId, out var tableSoList)) return;

            if (!tableSoList.tableSos.Contains(tableSo)) return;
            
            tableSoList.tableSos.Remove(tableSo);
            if (tableSoList.tableSos.Count <= 0)
                projectTableSoDic.Remove(tableId);

            EditorUtility.SetDirty(this);
        }

        public void RegisterTableFetch(Table table, RequestOperation requestOperation)
        {
            if(IsFetchingUpdate || _fetchingTablesDic.ContainsKey(table.name)) return;
            if(tableList.Find(match => match.id == table.id) == null) return;
            
            _fetchingTablesDic.Add(table.name, requestOperation);
            
            requestOperation.OnFinished += () =>
            {
                _fetchingTablesDic.Remove(table.name);
                if(_fetchingTablesDic.Count <= 0) OnAllTableSyncFinished?.Invoke();
            };
            OnTableSyncRequested?.Invoke(table.name, requestOperation);
        }

        public void SyncTables()
        {
            List<long> emptyKeys = new List<long>();
            foreach (var tableId in ProjectTableSoDic.Keys)
            {
                var tableSoList = ProjectTableSoDic[tableId].tableSos;
                
                for (int i = 0; i < tableSoList.Count;)
                {
                    var tableSo = tableSoList[i];
                    if (tableSo == null)
                    {
                        tableSoList.RemoveAt(i);
                        continue;
                    }
                    tableSo.FetchEntries();
                    i++;
                }
                
                if(tableSoList.Count <= 0) emptyKeys.Add(tableId);
            }

            foreach (var id in emptyKeys)
            {
                projectTableSoDic.Remove(id);
            }
        }
    }
}