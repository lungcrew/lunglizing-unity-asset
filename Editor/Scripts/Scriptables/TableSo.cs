using System.Collections.Generic;
using System.Linq;
using Lungfetcher.Data;
using Lungfetcher.Helper;
using UnityEditor;
using UnityEngine;

namespace Lungfetcher.Editor.Scriptables
{
    [CreateAssetMenu(fileName = "TableLungfetcher", menuName = "Lungfetcher/TableScriptable", order = 1)]
    public class TableSo : ScriptableObject
    {
        [SerializeField]private ProjectSo project;
        [SerializeField]private Table tableInfo;
        [SerializeField]private TableStrategy strategy;
        [SerializeField]private LocalizationAsset localizationTable;
        
        private LongEntryDictionary _entryDic;
        
        public ProjectSo Project => project;
        public Table TableInfo => tableInfo;
        public TableStrategy Strategy => strategy;
        public LocalizationAsset LocalizationTable => localizationTable;
        
        public enum TableStrategy
        {
            UUID,
            Custom
        }

        public void SetupTable(Table table)
        {
            UpdateTableInfo(table);
        }

        public void UpdateTableInfo(Table table)
        {
            tableInfo = table;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        public void UpdateEntries(List<Entry> entries, bool removeUnused = false)
        {
            if (_entryDic.Count <= 0)
            {
                foreach (Entry entry in entries)
                {
                    _entryDic.Add(entry.id, entry);
                }
            }
            else
            {
                if(removeUnused)
                    RemoveUnusedEntries(entries);
                
                foreach (Entry entry in entries)
                {
                    if (_entryDic.TryGetValue(entry.id, out var value))
                    {
                        _entryDic[entry.id] = entry;
                    }
                    else
                    {
                        _entryDic.Add(entry.id, entry);
                    }
                }
            }

        }

        public void UpdateEntry(Entry entry)
        {
            if (_entryDic.Count <= 0)
            {
                _entryDic.Add(entry.id, entry);
            }
            else
            {
                if (_entryDic.TryGetValue(entry.id, out var value))
                {
                    _entryDic[entry.id] = entry;
                }
                else
                {
                    _entryDic.Add(entry.id, entry);
                }
            }
        }
        
        public void RemoveUnusedEntries(List<Entry> entries)
        {
            List<KeyValuePair<long, Entry>> entryDicTempList = _entryDic.ToList();
            
            foreach (KeyValuePair<long, Entry> entry in entryDicTempList.Where(valuePair => 
                         entries.Find(x => x.id == valuePair.Key) == null))
            {
                RemoveEntry(entry.Key);
            }
        }

        public Entry GetEntry(long id)
        {
            return _entryDic.TryGetValue(id, out var value) ? value : null;
        }

        public void RemoveEntry(long id)
        {
            _entryDic.Remove(id);
        }

        public void RemoveAllEntries()
        {
            _entryDic.Clear();
        }
    }
}