﻿using System;
using System.Collections.Generic;
using Lungfetcher.Data;
using Lungfetcher.Editor.Helper;
using Lungfetcher.Editor.Operations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Logger = Lungfetcher.Helper.Logger;


namespace Lungfetcher.Editor.Scriptables
{
    [CreateAssetMenu(fileName = "ProjectLungfetcher", menuName = "Lungfetcher/ProjectScriptable", order = 1)]
    public class ProjectSo : ScriptableObject
    {
        #region Fields

        [SerializeField]private string apiKey = "QcS74OK.M4L77werD9BhsrGGPjixUgKwjVmFrfXQ";
        [SerializeField]private LongTablesSoDictionary projectTableSoDic;
        [SerializeField]private Project projectInfo;
        [SerializeField]private List<Table> tableList;
        [SerializeField]private string lastUpdate = "";
        [SerializeField]private List<LocaleField> projectLocales = new List<LocaleField>();
        
        private List<TableSo> _updatingTableSos = new List<TableSo>();
        
        #endregion

        #region Getters

        public string ApiKey => apiKey;
        public Project ProjectInfo => projectInfo;
        public List<Table> TableList => tableList;
        public LongTablesSoDictionary ProjectTableSoDic => projectTableSoDic;
        public List<TableSo> UpdatingTableSos => _updatingTableSos;
        public string LastUpdate => lastUpdate;
        public List<LocaleField> ProjectLocales => projectLocales;
        
        #endregion

        #region Properties

        public bool IsFetchingUpdate { get; private set; } = false;
        public UpdateProjectOperation UpdateProjectOperationRef { get; private set; }

        #endregion

        #region Enums
        
        private enum TableUpdateType
        {
            None,
            SoftSyncTable,
            HardSyncTable,
            ProjectUpdated,
            SyncLocales
        }
        
        #endregion

        #region Events
        
        public event UnityAction OnBeginProjectUpdate;
        public event UnityAction OnFinishProjectUpdate;
        public event UnityAction<TableSo, RequestOperation> OnTableSyncRequested;
        public event UnityAction OnAllTableSyncFinished;
        
        #endregion

        #region Updates

        public bool IsSyncingTableSos()
        {
            return _updatingTableSos.Count > 0;
        }
        
        public void FetchUpdate()
        {
            if(IsFetchingUpdate) return;

            IsFetchingUpdate = true; 
            OnBeginProjectUpdate?.Invoke();
            UpdateProjectOperationRef = new UpdateProjectOperation(this);
            UpdateProjectOperationRef.OnFinished += FinishFetch;
        }

        public void SyncTablesInfo(List<Table> tables)
        {
            tableList = tables;
            RemoveUnusedTablesFromDict();
            
            EditorUtility.SetDirty(this);
        }
        
        public void SyncProjectInfo(Project project)
        {
            projectInfo = project;
            
            if(projectInfo != null) UpdateLocales();
            
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

        private void UpdateLocales()
        {
            List<LocaleField> updatedLocales = new List<LocaleField>();
            foreach (var localeField in projectInfo.locales)
            {
                if (projectLocales.Count > 0)
                {
                    var foundLocaleField = projectLocales.Find(x => x.id == localeField.id);
                    if (foundLocaleField != null)
                    {
                        updatedLocales.Add(foundLocaleField);
                        foundLocaleField.UpdateLocaleSoftData(localeField);
                        continue;
                    }
                    updatedLocales.Add(new LocaleField(localeField));
                }
                else
                {
                    updatedLocales.Add(new LocaleField(localeField));
                }
            }
            
            projectLocales = updatedLocales;
            
            EditorUtility.SetDirty(this);
        }
        
        private void FinishFetch()
        {
            if (UpdateProjectOperationRef.IsFinishedSuccessfully || UpdateProjectOperationRef.Progress > 0)
            {
                UpdateTableSosProjectData();
                lastUpdate = DateTime.Now.ToString();
                EditorUtility.SetDirty(this);
            }

            UpdateProjectOperationRef = null;
            IsFetchingUpdate = false;
            OnFinishProjectUpdate?.Invoke();
        }
        
        #endregion

        #region TableSos Management

        public void AddTableSo(TableSo tableSo, long tableId)
        {
            if (projectInfo == null || tableList == null)
            {
                Logger.ProjectMissingInfo();
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
            
            EditorUtility.SetDirty(this);
        }

        public void SwitchTableSoId(TableSo tableSo, long oldTableId, long newTableId)
        {
            if (projectInfo == null || tableList == null)
            {
                Logger.ProjectMissingInfo();
                return;
            }
            RemoveTableSo(tableSo, oldTableId);
            AddTableSo(tableSo, newTableId);
        }

        public void RemoveTableSo(TableSo tableSo, long tableId)
        {
            if (projectInfo == null || tableList == null)
            {
                Logger.ProjectMissingInfo();
                return;
            }

            if (!projectTableSoDic.TryGetValue(tableId, out var tableSoList)) return;

            if (!tableSoList.tableSos.Contains(tableSo)) return;
            
            tableSoList.tableSos.Remove(tableSo);
            if (tableSoList.tableSos.Count <= 0)
                projectTableSoDic.Remove(tableId);

            EditorUtility.SetDirty(this);
        }

        public void RegisterTableUpdate(TableSo tableSo, UpdateTableOperation updateTableOperation)
        {
            if(IsFetchingUpdate || _updatingTableSos.Contains(tableSo)) return;
            
            _updatingTableSos.Add(tableSo);
            
            updateTableOperation.OnFinished += () =>
            {
                _updatingTableSos.Remove(tableSo);
                if(_updatingTableSos.Count <= 0) OnAllTableSyncFinished?.Invoke();
            };
            OnTableSyncRequested?.Invoke(tableSo, updateTableOperation);
        }
        
        private void UpdateTableSos(TableUpdateType updateType)
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
                    UpdateTableSo(tableSo, updateType);
                    i++;
                }
                
                if(tableSoList.Count <= 0) emptyKeys.Add(tableId);
            }

            foreach (var id in emptyKeys)
            {
                projectTableSoDic.Remove(id);
            }
        }

        private void UpdateTableSo(TableSo tableSo,TableUpdateType updateType)
        {
            switch (updateType)
            {
                case TableUpdateType.SoftSyncTable:
                    tableSo.FetchEntries(false);
                    break;
                case TableUpdateType.HardSyncTable:
                    tableSo.FetchEntries(true);
                    break;
                case TableUpdateType.ProjectUpdated:
                    tableSo.ProjectUpdated();
                    break;
                case TableUpdateType.SyncLocales:
                    tableSo.SetLocales(projectLocales);
                    break;
                case TableUpdateType.None:
                    break;
            }
        }

        private void ClearInvalidTableSos()
        {
            UpdateTableSos(TableUpdateType.None);
        }
        
        private void UpdateTableSosProjectData()
        {
            UpdateTableSos(TableUpdateType.ProjectUpdated);
        }

        public void SyncTableLocales()
        {
            UpdateTableSos(TableUpdateType.SyncLocales);
        }
        
        public void SyncTableSos(bool hardSync = false)
        {
            TableUpdateType updateType = hardSync ? TableUpdateType.HardSyncTable : TableUpdateType.SoftSyncTable;
            UpdateTableSos(updateType);
        }
        
        #endregion
    }
}