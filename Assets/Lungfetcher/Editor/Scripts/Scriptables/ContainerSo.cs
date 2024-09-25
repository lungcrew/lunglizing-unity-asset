using System;
using System.Collections.Generic;
using System.Globalization;
using Lungfetcher.Data;
using Lungfetcher.Editor.Operations;
using Lungfetcher.Helper;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using Logger = Lungfetcher.Helper.Logger;

namespace Lungfetcher.Editor.Scriptables
{
    [CreateAssetMenu(fileName = "LungContainer", menuName = "Lungfetcher/Lung Container", order = 1)]
    public class ContainerSo : ScriptableObject
    {
        #region Fields
        
        [SerializeField]private ProjectSo project;
        [SerializeField]private Container containerInfo;
        [SerializeField]private ContainerStrategy strategy;
        [SerializeField]private StringTableCollection stringTableCollection;
        [SerializeField]private string lastUpdate = "";
        
        private LongEntryDictionary _entryDic;
        
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
        
        public event UnityAction OnBeginContainerEntriesUpdate;
        public event UnityAction<bool> OnFinishContainerEntriesUpdate;
        public event UnityAction OnProjectDataUpdated;
        
        #endregion

        #region Enum
        
        public enum ContainerStrategy
        {
            UUID,
            ReadableKey
        }
        
        #endregion
        
        #region Methods
        
        private void Awake()
        {
            if (!project) return;

            long containerID = containerInfo?.id ?? 0;
            project.AddContainerSo(this, containerID);
        }

        public void ProjectChanged(ProjectSo newProject, ProjectSo oldProject)
        {
            if (oldProject != null && oldProject != newProject) 
                ChangeContainerInfo(null, oldProject);
            
            long containerID = containerInfo?.id ?? 0;
            if(oldProject != null) oldProject.RemoveContainerSo(this, containerID);
            if(newProject != null) newProject.AddContainerSo(this, containerID);
            
            lastUpdate = "";

            if (IsUpdatingEntries)
            {
                UpdateContainerOperationRef?.CancelOperation();
                UpdateContainerOperationRef = null;
                FinishContainerUpdate();
            }
        }
        

        public void ChangeContainerInfo(Container container, ProjectSo projectSo = null)
        {
            if (projectSo == null) projectSo = project;
            Container oldContainerInfo = containerInfo;
            containerInfo = container;
            long newContainerId = container?.id ?? 0;
            long oldContainerId = oldContainerInfo?.id ?? 0;
            if(!projectSo) return;
            
            projectSo.SwitchContainerSoId(this, oldContainerId ,newContainerId);

            lastUpdate = "";
            
            EditorUtility.SetDirty(this);
        }

        public void ProjectUpdated()
        {
            if (containerInfo.id == 0) return;

            var container = project.ContainerList.Find(target => target.id == containerInfo.id);
            containerInfo = container;
            
            OnProjectDataUpdated?.Invoke();
            EditorUtility.SetDirty(this);
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

    [Serializable]
    public class ContainerSoList
    {
        public List<ContainerSo> containerSos = new List<ContainerSo>();
    }
}