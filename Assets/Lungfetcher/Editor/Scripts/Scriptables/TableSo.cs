using System;
using System.Collections.Generic;
using Lungfetcher.Data;
using Lungfetcher.Helper;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Lungfetcher.Editor.Scriptables
{
    [CreateAssetMenu(fileName = "TableLungfetcher", menuName = "Lungfetcher/TableScriptable", order = 1)]
    public class TableSo : ScriptableObject
    {
        [SerializeField]private ProjectSo project;
        [SerializeField]private Table tableInfo;
        [SerializeField]private TableStrategy strategy;
        [SerializeField]private StringTableCollection stringTableCollection;
        [SerializeField]private string lastUpdate = "";
        [SerializeField]private List<LocaleField> locales = new List<LocaleField>();
        
        private LongEntryDictionary _entryDic;
        public ProjectSo Project => project;
        public Table TableInfo => tableInfo;
        public TableStrategy Strategy => strategy;
        public StringTableCollection StringTableCollection => stringTableCollection;
        public string LastUpdate => lastUpdate;
        public List<LocaleField> Locales => locales;
        public bool IsUpdatingEntries { get; private set; } = false;
        public FetchOperation<List<EntriesLocale>> FetchEntriesOperation { get; private set; }
        
        public event UnityAction OnBeginEntryUpdate;
        public event UnityAction OnFinishEntryUpdate;
        
        public enum TableStrategy
        {
            UUID,
            Custom
        }

        public void ProjectChanged(ProjectSo newProject, ProjectSo oldProject)
        {
            if(tableInfo == null) return;
            
            if (oldProject != null && oldProject != newProject) 
                UpdateTableInfo(null, oldProject);

            if (IsUpdatingEntries)
            {
                FetchEntriesOperation = null;
                FinishEntryFetch();
            }
        }
        

        public void UpdateTableInfo(Table table, ProjectSo projectSo = null)
        {
            if (projectSo == null) projectSo = project;
            Table oldTableInfo = tableInfo;
            tableInfo = table;
            if(!projectSo) return;
            if (oldTableInfo.id != 0)
            {
                if (table!= null && table.id != 0) 
                    projectSo.SwitchTableSoId(this, oldTableInfo.id ,table.id);

                if (table== null || table.id == 0) 
                    projectSo.RemoveTableSo(this, oldTableInfo.id);
            }
            else
            {
                if (table != null) 
                    projectSo.AddTableSo(this, table.id);
            }
            
            EditorUtility.SetDirty(this);
        }
        
        public void UpdateLocales()
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

        public void FetchEntries()
        {
            if(!project || tableInfo == null || IsUpdatingEntries) return;
            
            FetchEntriesOperation = OperationsController.RequestFetchTableEntries(tableInfo.id, project.ApiKey);
            FetchEntriesOperation.OnResponse += CreateEntries;
            FetchEntriesOperation.OnFinished += FinishEntryFetch;
            
            IsUpdatingEntries = true;
            OnBeginEntryUpdate?.Invoke();
            
            project.RegisterTableFetch(tableInfo, FetchEntriesOperation);
        }

        private void CreateEntries(List<EntriesLocale> entriesLocales)
        {
            if(!stringTableCollection || entriesLocales == null) return;
            stringTableCollection.ClearAllEntries();
            
            foreach (var entryLocale in entriesLocales)
            {
                var localeField = locales.Find(locale => locale.id == entryLocale.locale_id);
                if(localeField == null || !localeField.Locale) continue;

                var localizationTable = stringTableCollection.GetTable(localeField.Locale.Identifier);
                if (!localizationTable)
                {
                    localizationTable = stringTableCollection.AddNewTable(localeField.Locale.Identifier);
                }
                
                var stringTable = localizationTable as StringTable;
                if(!stringTable) return;
                foreach (var entry in entryLocale.localizations)
                {
                    string key = entry.entry_readable_key;
                    if(strategy == TableStrategy.UUID || string.IsNullOrEmpty(key))
                        key = entry.entry_uuid;
                    
                    stringTable.AddEntry(key, entry.text);
                }
            }
            
            EditorUtility.SetDirty(stringTableCollection);
            EditorUtility.SetDirty(stringTableCollection.SharedData);

            if (EditorWindow.HasOpenInstances<LocalizationTablesWindow>())
            {
                var wnd = EditorWindow.GetWindow(typeof(LocalizationTablesWindow));
                if (!wnd) return;
                wnd.Close();
                LocalizationTablesWindow.ShowWindow(stringTableCollection);
            }
            else
            {
                LocalizationTablesWindow.ShowWindow(stringTableCollection);
            }
        }

        private void UpdateEntry(LocalizedEntry entry, StringTable stringTable)
        {
            if (strategy == TableStrategy.UUID)
            {
                if(string.IsNullOrWhiteSpace(entry.entry_readable_key)) return;
                
                var entryKey = stringTable.GetEntry(entry.entry_readable_key);
                entryKey?.RemoveFromTable();
                
                stringTable.AddEntry(entry.entry_uuid, entry.text);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(entry.entry_readable_key))
                {
                    stringTable.AddEntry(entry.entry_uuid, entry.text);
                }
                else
                {
                    var entryUuid = stringTable.GetEntry(entry.entry_uuid);
                    entryUuid?.RemoveFromTable();
                    
                    stringTable.AddEntry(entry.entry_readable_key, entry.text);
                }
            }
        }
       

        private void FinishEntryFetch()
        {
            FetchEntriesOperation.OnFinished -= FinishEntryFetch;
            FetchEntriesOperation.OnResponse -= CreateEntries;
            FetchEntriesOperation = null;

            if (!IsUpdatingEntries) return;
            IsUpdatingEntries = false;
            OnFinishEntryUpdate?.Invoke();
        }
    }

    [Serializable]
    public class TableSoList
    {
        public List<TableSo> tableSos = new List<TableSo>();
    }

    [Serializable]
    public class LocaleField
    {
        [SerializeField]private Locale locale;

        public string code;
        public long id;
        public string name;

        public Locale Locale => locale;

        public LocaleField(ProjectLocale projectLocale)
        {
            name = projectLocale.name;
            id = projectLocale.id;
            code = projectLocale.code;
        }
    }
}