using System.Collections.Generic;
using System.Threading.Tasks;
using Lungfetcher.Data;
using Lungfetcher.Editor.Scriptables;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;
using Logger = Lungfetcher.Helper.Logger;

namespace Lungfetcher.Editor.Operations
{
	public class UpdateTableOperation : RequestOperation
	{
		private TableSo _tableSo;
		private bool _hardSync = false;
		private int _loopLimit;
		private int _entryMissingKeyCount = 0;
		private float _downloadFinishProgress = 20f;
		private float _createEntriesProgress = 80f;
		private FetchOperation<List<EntriesLocale>> _requestFetchTableEntries;

		public UpdateTableOperation(TableSo tableSo, bool hardSync = false, int loopLimit = 1000)
		{
			_loopLimit = loopLimit;
			_tableSo = tableSo;
			_hardSync = hardSync;
			UpdateTableEntries();
		}
		
		private async void UpdateTableEntries()
		{
			GenerateCancellationToken();
			bool updateSuccess = await FetchTableEntries();

			if (!updateSuccess)
			{
				if(_requestFetchTableEntries.IsCanceled)
					Logger.LogError("Table Entries Fetch Update Cancelled For Table " + 
					                _tableSo.name, _tableSo);
				else
				{
					Logger.LogError("Table Entries Fetch Update Failed For Table " + 
					                _tableSo.name, _tableSo);
				}
				
				FinishOperation(false);
				return;
			}
			
			UpdateProgress(_downloadFinishProgress);

			bool entriesSuccess = await CreateTableEntriesLoop(_requestFetchTableEntries.ResponseData);

			if (!entriesSuccess)
			{
				if(cancellationToken.IsCancellationRequested)
					Logger.LogError("Entries Update Cancelled at " + _tableSo.name, _tableSo);
				else
				{
					Logger.LogError("Entries Update Failed at " + _tableSo.name, _tableSo);
				}
				FinishOperation(false);
				return;
			}
			
			FinishOperation(true);
		}
		

		private async Task<bool> FetchTableEntries()
		{
			_requestFetchTableEntries = OperationsController.RequestFetchTableEntries(_tableSo.TableInfo.id, 
				_tableSo.Project.ApiKey);
			
			while (!_requestFetchTableEntries.IsFinished)
			{
				await Task.Yield();
				
				if(cancellationToken.IsCancellationRequested)
				{
					return false;
				}
			}

			return _requestFetchTableEntries.IsFinishedSuccessfully;
		}

		private async Task<bool> CreateTableEntriesLoop(List<EntriesLocale> entriesLocales)
		{
			int loopCount = 0;
			List<StringTable> updatedTables = new List<StringTable>();

			if (_hardSync)
			{
				_tableSo.StringTableCollection.ClearAllEntries();
			}
			
            foreach (var entryLocale in entriesLocales)
            {
	            var localeField = _tableSo.Locales.Find(locale => locale.id == entryLocale.locale_id);
	            if (localeField == null || !localeField.Locale) continue;
            
	            var localizationTable = _tableSo.StringTableCollection.GetTable(localeField.Locale.Identifier);
	            if (!localizationTable)
	            {
		            localizationTable = _tableSo.StringTableCollection.AddNewTable(localeField.Locale.Identifier);
	            }
                            
	            var stringTable = localizationTable as StringTable;
	            if(!stringTable) return false;
	            
	            updatedTables.Add(stringTable);
	            foreach (var entry in entryLocale.localizations)
	            {
		            CreateEntry(entry, stringTable, _hardSync);
		            
		            loopCount++;
		            
		            if (loopCount < _loopLimit) continue;
		            
		            await Task.Yield();
		            
		            if(cancellationToken.IsCancellationRequested)
		            {
			            return false;
		            }
		            
		            loopCount = 0;
	            }
	            
            }
            if(progress < _createEntriesProgress)
				UpdateProgress(_createEntriesProgress);
            
            await Task.Yield();
            
            if(_hardSync)
	            RemoveUnusedTables(updatedTables);

            if (_entryMissingKeyCount > 0)
	            Logger.LogWarning($"Readable Key missing in {_entryMissingKeyCount} Entries Of " +
	                              $"{_tableSo.name}! Used UUID Instead");

            return true;
		}

		private void CreateEntry(LocalizedEntry entry, StringTable stringTable, bool syncAll = false)
		{
			string key;
			if (_tableSo.Strategy == TableSo.TableStrategy.UUID)
			{
				key = entry.entry_uuid;
				
				if(!syncAll)
					//rename entry key if saved previously with readable key
					if (_tableSo.StringTableCollection.SharedData.GetEntry(entry.entry_readable_key) != null && 
					    _tableSo.StringTableCollection.SharedData.GetEntry(entry.entry_uuid) == null)
					{
						_tableSo.StringTableCollection.SharedData.RenameKey(entry.entry_readable_key,key);
					}
			}
			else
			{
				if (string.IsNullOrEmpty(entry.entry_readable_key))
				{
					_entryMissingKeyCount++;
					key = entry.entry_uuid;
				}
				else
				{
					key = entry.entry_readable_key;
					
					if(!syncAll)
						//rename entry key if saved previously with uuid
						if (_tableSo.StringTableCollection.SharedData.GetEntry(entry.entry_readable_key) == null && 
						    _tableSo.StringTableCollection.SharedData.GetEntry(entry.entry_uuid) != null)
						{
							_tableSo.StringTableCollection.SharedData.RenameKey(entry.entry_uuid, key);
						}
				}
			}
                                
			stringTable.AddEntry(key, entry.text);
		}

		private void RemoveUnusedTables(List<StringTable> updatedTables)
		{
			if(updatedTables.Count >= _tableSo.StringTableCollection.StringTables.Count) return;
			
			List<string> unusedTablesPath = new List<string>();
			foreach (var stringTable in _tableSo.StringTableCollection.StringTables)
			{
				if(updatedTables.Contains(stringTable)) continue;

				_tableSo.StringTableCollection.RemoveTable(stringTable);
				unusedTablesPath.Add(AssetDatabase.GetAssetPath(stringTable));
			}
			
			if(unusedTablesPath.Count <= 0) return;
			
			List<string> failedPath = new List<string>();
			AssetDatabase.DeleteAssets(unusedTablesPath.ToArray(), failedPath);

			if (failedPath.Count > 0)
			{
				Logger.LogWarning($"Failed to delete unused tables at: {string.Join(", ", failedPath)}");
			}
		}
	}
}