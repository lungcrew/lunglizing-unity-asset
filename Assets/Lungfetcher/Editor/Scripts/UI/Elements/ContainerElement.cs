using System;
using Lungfetcher.Editor.Operations;
using Lungfetcher.Editor.Scriptables;
using UnityEditor;
using UnityEditor.Localization.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Lungfetcher.Helper.Logger;

namespace Lungfetcher.Editor.UI.Elements
{
	public class ContainerElement : VisualElement
	{
		#region Variables

		private const string XMLPath = "UI Documents/ContainerElement";
		private ContainerSo _containerSo;
		private ProjectSo _projectSo;
		private long _containerId;
		private bool _showWindow;

		#endregion

		#region UXML References

		private ObjectField TableCollection => this.Q<ObjectField>("table-reference-object");
		private Foldout ContainerFoldout => this.Q<Foldout>("container-foldout");
		private Label UpdateLabel => this.Q<Label>("updated-label");
		private EnumField StrategyEnum => this.Q<EnumField>("strategy-enum");
		private ScrollView ProgressView => this.Q<ScrollView>("progress-view");
		private Button SyncContainerButton => this.Q<Button>("sync-container-btn");
		private Button HardSyncContainerButton => this.Q<Button>("hard-sync-container-btn");

		#endregion

		#region Events

		public event Action<long> OnElementAutoRemoved;

		#endregion

		#region Constructor/Setup

		public ContainerElement(ContainerSo containerSo)
		{
			var asset = Resources.Load<VisualTreeAsset>(XMLPath);
			asset.CloneTree(this);
			
			SetupElement(containerSo);
		}

		private void SetupElement(ContainerSo containerSo)
		{
			if(containerSo == null) return;
			_containerSo = containerSo;
			_projectSo = _containerSo.Project;
			_containerId = _containerSo.ContainerInfo.id;
			
			var serializedObject = new SerializedObject(_containerSo);
			
			StrategyEnum.BindProperty(serializedObject.FindProperty("strategy"));
			TableCollection.BindProperty(serializedObject.FindProperty("stringTableCollection"));
			
			RefreshFoldoutLabel();
			RefreshUpdateLabel();
			RefreshSyncContainerEntriesButtons();
			RefreshEntryFetchProgress();
			
			SetListeners();
		}

		#endregion

		#region Buttons

		private void HardSyncContainer()
		{
			if(IsContainerNull()) return;
			
			_showWindow = true;
			_containerSo.FetchEntries(hardSync:true);
			RefreshEntryFetchProgress();
			RefreshSyncContainerEntriesButtons();
		}

		private void SyncContainer()
		{
			if(IsContainerNull()) return;
			
			_showWindow = true;
			_containerSo.FetchEntries(hardSync:false);
			RefreshEntryFetchProgress();
			RefreshSyncContainerEntriesButtons();
		}
		
		private void RefreshSyncContainerEntriesButtons()
		{
			if(IsContainerNull()) return;
			
			if (_containerSo.ContainerInfo == null || _containerSo.ContainerInfo.id == 0)
			{
				SyncContainerButton?.SetEnabled(false);
				HardSyncContainerButton?.SetEnabled(false);
				return;
			}

			if (_containerSo.Project.IsFetchingUpdate || _containerSo.IsUpdatingEntries)
			{
				SyncContainerButton?.SetEnabled(false);
				HardSyncContainerButton?.SetEnabled(false);
				return;
			}

			HardSyncContainerButton?.SetEnabled(true);
			SyncContainerButton?.SetEnabled(true);
		}
		
		#endregion

		#region Element Updates

		private void RefreshFoldoutLabel() => ContainerFoldout.text = _containerSo.ContainerInfo.name;
		
		private void RefreshEntryFetchProgress()
		{
			ProgressView.Clear();

			if (_containerSo.UpdateContainerOperationRef == null) return;
			
			ProgressBar progressBar = CreateUpdateContainerProgressBar(_containerSo.UpdateContainerOperationRef);
			progressBar.title = "Syncing Entries";
			ProgressView.Add(progressBar);
		}

		private ProgressBar CreateUpdateContainerProgressBar(UpdateContainerOperation updateContainerOperation)
		{
			ProgressBar progressBar = new ProgressBar
			{
				value = updateContainerOperation.Progress
			};
			progressBar.schedule.Execute(() =>
			{
				progressBar.value = updateContainerOperation.Progress;
				if (updateContainerOperation.IsFinished)
					progressBar.title = progressBar.title + " " +
					                    (updateContainerOperation.IsFinishedSuccessfully ? "Done" : "Failed");
			}).Until(() => updateContainerOperation.IsFinished);

			return progressBar;
		}

		private void RefreshUpdateLabel()
		{
			UpdateLabel.text = string.IsNullOrEmpty(_containerSo.LastUpdate)
				? "" : "Last Synced at: " + _containerSo.LastUpdate;
		}

		private void ProjectUpdated()
		{
			if(IsContainerNull()) return;

			RefreshFoldoutLabel();
			RefreshSyncContainerEntriesButtons();
		}
		
		private void ContainerEntriesUpdated(bool success)
		{
			RefreshSyncContainerEntriesButtons();

			if (!success) return;
            
			if(_showWindow)
				OpenEditorWindow();
			_showWindow = false;
		}

		private void OpenEditorWindow()
		{
			if (EditorWindow.HasOpenInstances<LocalizationTablesWindow>())
			{
				var editorWindow = EditorWindow.GetWindow(typeof(LocalizationTablesWindow));
				if (editorWindow.docked)
				{
					Logger.Log($"Table window of container {_containerSo.ContainerInfo.name} is docked, " +
					           $"reload to see changes");
					return;
				}
				if(editorWindow)
				{
					editorWindow.Close();
					LocalizationTablesWindow.ShowWindow(_containerSo.StringTableCollection);
				}
				else
				{
					LocalizationTablesWindow.ShowWindow(_containerSo.StringTableCollection);
				}
			}
			else
			{
				LocalizationTablesWindow.ShowWindow(_containerSo.StringTableCollection);
			}
		}

		private bool IsContainerNull()
		{
			if(_containerSo) return false;
			Cleanup();
			if(parent.Contains(this))
				parent.Remove(this);
			
			OnElementAutoRemoved?.Invoke(_containerId);
			return true;
		}

		#endregion

		#region Listeners

		public void Cleanup() => RemoveListeners();
		
		private void SetListeners()
		{
			SyncContainerButton.clicked += SyncContainer;
			HardSyncContainerButton.clicked += HardSyncContainer;
			
			if(_containerSo == null) return;
			
			_containerSo.OnFinishContainerEntriesUpdate += ContainerEntriesUpdated;

			if (_projectSo == null) return;

			_projectSo.OnFinishProjectUpdate += ProjectUpdated;
			_projectSo.OnBeginProjectUpdate += RefreshSyncContainerEntriesButtons;
		}

		private void RemoveListeners()
		{
			SyncContainerButton.clicked -= SyncContainer;
			HardSyncContainerButton.clicked -= HardSyncContainer;
			
			if(_containerSo == null) return;
			
			_containerSo.OnFinishContainerEntriesUpdate -= ContainerEntriesUpdated;

			if (_containerSo.Project == null) return;

			_projectSo.OnFinishProjectUpdate -= ProjectUpdated;
			_projectSo.OnBeginProjectUpdate -= RefreshSyncContainerEntriesButtons;
		}

		#endregion
	}
}