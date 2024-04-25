using System;
using System.Collections.Generic;
using Lungfetcher.Data;
using Lungfetcher.Editor.Operations;
using Lungfetcher.Helper;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;

namespace Lungfetcher.Editor.Scriptables
{
    [CreateAssetMenu(fileName = "TableLungfetcher", menuName = "Lungfetcher/TableScriptable", order = 1)]
    public class TableSo : ScriptableObject
    {
        #region Fields
        
        [SerializeField]private ProjectSo project;
        [SerializeField]private Table tableInfo;
        [SerializeField]private TableStrategy strategy;
        [SerializeField]private StringTableCollection stringTableCollection;
        [SerializeField]private string lastUpdate = "";
        [SerializeField]private List<LocaleField> locales = new List<LocaleField>();
        
        private LongEntryDictionary _entryDic;
        
        #endregion
        
        #region Getters
        
        public ProjectSo Project => project;
        public Table TableInfo => tableInfo;
        public TableStrategy Strategy => strategy;
        public StringTableCollection StringTableCollection => stringTableCollection;
        public string LastUpdate => lastUpdate;
        public List<LocaleField> Locales => locales;
        
        #endregion
        
        #region Properties
        
        public bool IsUpdatingEntries { get; private set; } = false;
        public UpdateTableOperation UpdateTableOperationRef { get; private set; }
        
        #endregion
        
        #region Events
        
        public event UnityAction OnBeginTableEntriesUpdate;
        public event UnityAction<bool> OnFinishTableEntriesUpdate;
        public event UnityAction OnProjectDataUpdated;
        
        #endregion

        #region Enum
        
        public enum TableStrategy
        {
            UUID,
            ReadableKey
        }
        
        #endregion
        
        #region Methods

        public void ProjectChanged(ProjectSo newProject, ProjectSo oldProject)
        {
            if (oldProject != null && oldProject != newProject) 
                ChangeTableInfo(null, oldProject);
            
            long tableId = tableInfo?.id ?? 0;
            if(oldProject != null) oldProject.RemoveTableSo(this, tableId);
            if(newProject != null) newProject.AddTableSo(this, tableId);
            
            lastUpdate = "";

            if (IsUpdatingEntries)
            {
                UpdateTableOperationRef?.CancelOperation();
                UpdateTableOperationRef = null;
                FinishTableUpdate();
            }
            
            UpdateLocales();
        }
        

        public void ChangeTableInfo(Table table, ProjectSo projectSo = null)
        {
            if (projectSo == null) projectSo = project;
            Table oldTableInfo = tableInfo;
            tableInfo = table;
            long newTableId = table?.id ?? 0;
            long oldTableId = oldTableInfo?.id ?? 0;
            if(!projectSo) return;
            
            projectSo.SwitchTableSoId(this, oldTableId ,newTableId);

            lastUpdate = "";
            
            EditorUtility.SetDirty(this);
        }

        private void UpdateLocales()
        {
            if (!project) return;
            
            List<LocaleField> updatedLocales = new List<LocaleField>();
            
            foreach (var localeInfo in project.ProjectInfo.locales)
            {
                if (locales.Count > 0)
                {
                    var foundLocaleField = locales.Find(x => x.id == localeInfo.id);
                    if (foundLocaleField != null)
                    {
                        updatedLocales.Add(foundLocaleField);
                        foundLocaleField.UpdateLocaleSoftData(localeInfo);
                        continue;
                    }
                    updatedLocales.Add(new LocaleField(localeInfo));
                }
                else
                {
                    updatedLocales.Add(new LocaleField(localeInfo));
                }
            }
            
            locales = updatedLocales;
            EditorUtility.SetDirty(this);
        }

        public void SetLocales(List<LocaleField> localeFields)
        {
            locales = localeFields;
            EditorUtility.SetDirty(this);
        }

        public void ProjectUpdated()
        {
            UpdateLocales();

            if (tableInfo.id == 0) return;

            var table = project.TableList.Find(target => target.id == tableInfo.id);
            tableInfo = table;
            
            OnProjectDataUpdated?.Invoke();
            EditorUtility.SetDirty(this);
        }
        
        #endregion
        
        #region Updates

        public void FetchEntries(bool hardSync = false)
        {
            if(!project || tableInfo == null || IsUpdatingEntries) return;
            if(tableInfo.id == 0) return;
            
            UpdateTableOperationRef?.CancelOperation();
            UpdateTableOperationRef = new UpdateTableOperation(this, hardSync);
            
            UpdateTableOperationRef.OnFinished += FinishTableUpdate;
            
            IsUpdatingEntries = true;
            OnBeginTableEntriesUpdate?.Invoke();
            
            project.RegisterTableUpdate(this, UpdateTableOperationRef);
        }
        
        private void FinishTableUpdate()
        {
            bool success = UpdateTableOperationRef.IsFinishedSuccessfully;
            UpdateTableOperationRef.OnFinished -= FinishTableUpdate;
            if (UpdateTableOperationRef.IsFinishedSuccessfully)
            {
                lastUpdate = DateTime.Now.ToString();
                EditorUtility.SetDirty(this);
            }
            
            UpdateTableOperationRef = null;

            if (!IsUpdatingEntries) return;
            IsUpdatingEntries = false; 
            OnFinishTableEntriesUpdate?.Invoke(success);
        }
        
        #endregion
    }

    [Serializable]
    public class TableSoList
    {
        public List<TableSo> tableSos = new List<TableSo>();
    }
}