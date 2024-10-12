using System;
using System.Collections.Generic;
using System.Globalization;
using Lungfetcher.Data;
using Lungfetcher.Editor.Operations;
using Lungfetcher.Helper;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using Logger = Lungfetcher.Helper.Logger;

namespace Lungfetcher.Editor.Scriptables
{
    public class ContainerSo : ScriptableObject
    {
        #region Fields
        
        [SerializeField, HideInInspector]private ProjectSo project;
        [SerializeField, HideInInspector]private Container containerInfo;
        [SerializeField, HideInInspector]private ContainerStrategy strategy = ContainerStrategy.ReadableKey;
        [SerializeField, HideInInspector]private StringTableCollection stringTableCollection;
        [SerializeField, HideInInspector]private string lastUpdate = "";
        
        #endregion
        
        #region Getters
        
        public ProjectSo Project => project;
        public Container ContainerInfo => containerInfo;
        public ContainerStrategy Strategy => strategy;
        public StringTableCollection StringTableCollection => stringTableCollection;
        public string LastUpdate => lastUpdate;
        
        #endregion
        
        #region Properties
        
        public bool IsUpdatingEntries { get; private set; } = false;
        public UpdateContainerOperation UpdateContainerOperationRef { get; private set; }
        
        #endregion
        
        #region Events
        
        public event Action OnBeginContainerEntriesUpdate;
        public event Action<bool> OnFinishContainerEntriesUpdate;
        public event Action OnContainerInfoUpdated; 
        
        #endregion

        #region Enum
        
        public enum ContainerStrategy
        {
            UUID,
            ReadableKey
        }
        
        #endregion
        
        #region Setups

        public void Init(Container container, ProjectSo projectSo)
        {
            containerInfo = container;
            project = projectSo;
            EditorUtility.SetDirty(this);
        }
        
        public void UpdateContainerInfo(Container container)
        {
            containerInfo = container;
            OnContainerInfoUpdated?.Invoke();
        }
        
        #endregion
        
        #region Updates

        public void FetchEntries(bool hardSync = false)
        {
            if(!project || containerInfo == null || IsUpdatingEntries) return;
            if (containerInfo.id == 0)
            {
                Logger.LogError($"Container info not set for container {this.name}", this);
                return;
            }
            if (!stringTableCollection)
            {
                Logger.LogError($"string table collection reference missing for container {this.name}", this);
                return;
            }
            
            UpdateContainerOperationRef?.CancelOperation();
            UpdateContainerOperationRef = new UpdateContainerOperation(this, hardSync);
            
            UpdateContainerOperationRef.OnFinished += FinishContainerUpdate;
            
            IsUpdatingEntries = true;
            OnBeginContainerEntriesUpdate?.Invoke();
            
            project.RegisterContainerUpdate(this, UpdateContainerOperationRef);
        }
        
        private void FinishContainerUpdate()
        {
            bool success = UpdateContainerOperationRef.IsFinishedSuccessfully;
            UpdateContainerOperationRef.OnFinished -= FinishContainerUpdate;
            if (UpdateContainerOperationRef.IsFinishedSuccessfully)
            {
                lastUpdate = DateTime.Now.ToString(CultureInfo.CurrentCulture);
                EditorUtility.SetDirty(this);
            }
            
            UpdateContainerOperationRef = null;

            if (!IsUpdatingEntries) return;
            IsUpdatingEntries = false; 
            OnFinishContainerEntriesUpdate?.Invoke(success);
        }
        
        #endregion
    }
}