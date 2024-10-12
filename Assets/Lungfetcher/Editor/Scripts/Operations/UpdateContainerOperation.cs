using System.Collections.Generic;
using System.Threading.Tasks;
using Lungfetcher.Data;
using Lungfetcher.Editor.Scriptables;
using UnityEditor;
using UnityEngine.Localization.Tables;
using Logger = Lungfetcher.Helper.Logger;

namespace Lungfetcher.Editor.Operations
{
	public class UpdateContainerOperation : RequestOperation
	{
		#region Variables

		private ContainerSo _containerSo;
		private bool _hardSync = false;
		private int _loopLimit;
		private int _entryMissingKeyCount = 0;
		private float _downloadFinishProgress = 20f;
		private float _createEntriesProgress = 80f;
		private List<string> _readableKeysCreated = new List<string>();
		private FetchOperation<List<EntriesLocale>> _requestFetchStringTableEntries;

		#endregion

		#region Constructor

		public UpdateContainerOperation(ContainerSo containerSo, bool hardSync = false, int loopLimit = 5000)
		{
			_loopLimit = loopLimit;
			_containerSo = containerSo;
			_hardSync = hardSync;
			UpdateStringTableEntries();
		}

		#endregion

		#region Async/Tasks

		private async void UpdateStringTableEntries()
		{
			GenerateCancellationToken();
			bool updateSuccess = await FetchStringTableEntries();

			if (!updateSuccess)
			{
				if(_requestFetchStringTableEntries.IsCanceled)
					Logger.LogError("String Table Entries Fetch Update Cancelled For Container " + 
					                _containerSo.name, _containerSo);
				else
				{
					Logger.LogError("String Table Entries Fetch Update Failed For Container " + 
					                _containerSo.name, _containerSo);
				}
				
				FinishOperation(false);
				return;
			}
			
			UpdateProgress(_downloadFinishProgress);

			bool entriesSuccess = await CreateStringTableEntriesLoop(_requestFetchStringTableEntries.ResponseData);

			if (!entriesSuccess)
			{
				if(cancellationToken.IsCancellationRequested)
					Logger.LogError("Entries Update Cancelled at " + _containerSo.name, _containerSo);
				else
				{
					Logger.LogError("Entries Update Failed at " + _containerSo.name, _containerSo);
				}
				FinishOperation(false);
				return;
			}
			
			FinishOperation(true);
		}
		

		private async Task<bool> FetchStringTableEntries()
		{
			_requestFetchStringTableEntries = OperationsController.RequestFetchContainersEntries
				(_containerSo.ContainerInfo.id, _containerSo.Project.ApiKey);
			
			while (!_requestFetchStringTableEntries.IsFinished)
			{
				await Task.Yield();
				
				if(cancellationToken.IsCancellationRequested)
				{
					return false;
				}
			}

			return _requestFetchStringTableEntries.IsFinishedSuccessfully;
		}

		private async Task<bool> CreateStringTableEntriesLoop(List<EntriesLocale> entriesLocales)
		{
			int loopCount = 0;
			List<StringTable> updatedStringTables = new List<StringTable>();
			
			if(_containerSo.Strategy == ContainerSo.ContainerStrategy.ReadableKey)
				_readableKeysCreated = new List<string>();

			if (_hardSync)
			{
				_containerSo.StringTableCollection.ClearAllEntries();
			}
			
			foreach (var entryLocale in entriesLocales)
			{
				var localeField = _containerSo.Project.ProjectLocales.Find(locale => 
					locale.id == entryLocale.locale.id);
				if (localeField == null || !localeField.Locale) continue;
            
				var localizationTable = _containerSo.StringTableCollection.GetTable(localeField.Locale.Identifier);
				if (!localizationTable)
				{
					localizationTable = _containerSo.StringTableCollection.AddNewTable(localeField.Locale.Identifier);
				}
                            
				var stringTable = localizationTable as StringTable;
				if(!stringTable) return false;
	            
				updatedStringTables.Add(stringTable);
				foreach (var entry in entryLocale.localizations)
				{
					CreateEntry(entry, stringTable, _hardSync);

					loopCount++;

					if (loopCount < _loopLimit) continue;

					await Task.Yield();

					if (cancellationToken.IsCancellationRequested)
						return false;

					loopCount = 0;
				}
	            
			}
			if(progress < _createEntriesProgress)
				UpdateProgress(_createEntriesProgress);
            
			await Task.Yield();
            
			if(_hardSync)
				RemoveUnusedStringTables(updatedStringTables);

			if (_entryMissingKeyCount > 0)
				Logger.LogWarning($"Readable Key missing in {_entryMissingKeyCount} Entries Of " +
				                  $"{_containerSo.name}! Used UUID Instead");

			return true;
		}

		#endregion

		#region Entries/Tables Management

		private void CreateEntry(LocalizedEntry entry, StringTable stringTable, bool syncAll = false)
		{
			string key;
			if (_containerSo.Strategy == ContainerSo.ContainerStrategy.UUID)
			{
				key = entry.entry_uuid;
				
				if(!syncAll)
					//rename entry key if saved previously with readable key
					if (_containerSo.StringTableCollection.SharedData.GetEntry(entry.entry_readable_key) != null && 
					    _containerSo.StringTableCollection.SharedData.GetEntry(entry.entry_uuid) == null)
					{
						_containerSo.StringTableCollection.SharedData.RenameKey(entry.entry_readable_key
							, key);
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
					
					//check for duplicate keys
					if(_readableKeysCreated.Contains(key))
						Logger.LogWarning($"Duplicate readable key {key} in {_containerSo.name}", 
							_containerSo);
					else
						_readableKeysCreated.Add(key);
					
					if(!syncAll)
						//rename entry key if saved previously with uuid
						if (_containerSo.StringTableCollection.SharedData.GetEntry(entry.entry_readable_key) == null && 
						    _containerSo.StringTableCollection.SharedData.GetEntry(entry.entry_uuid) != null)
						{
							_containerSo.StringTableCollection.SharedData.RenameKey(entry.entry_uuid, 
								key);
						}
				}
			}
                                
			stringTable.AddEntry(key, entry.text);
		}

		private void RemoveUnusedStringTables(List<StringTable> updatedStringTables)
		{
			if(updatedStringTables.Count >= _containerSo.StringTableCollection.StringTables.Count) return;
			
			List<string> unusedTablesPath = new List<string>();
			foreach (var stringTable in _containerSo.StringTableCollection.StringTables)
			{
				if(updatedStringTables.Contains(stringTable)) continue;

				_containerSo.StringTableCollection.RemoveTable(stringTable);
				unusedTablesPath.Add(AssetDatabase.GetAssetPath(stringTable));
			}
			
			if(unusedTablesPath.Count <= 0) return;
			
			List<string> failedPath = new List<string>();
			AssetDatabase.DeleteAssets(unusedTablesPath.ToArray(), failedPath);

			if (failedPath.Count > 0)
			{
				Logger.LogWarning($"Failed to delete unused string tables at: {string.Join(", ", failedPath)}");
			}
		}

		#endregion
	}
}