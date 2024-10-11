using System;
using System.Collections.Generic;
using System.Globalization;
using Lungfetcher.Data;
using Lungfetcher.Editor.Helper;
using Lungfetcher.Editor.Operations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Logger = Lungfetcher.Helper.Logger;


namespace Lungfetcher.Editor.Scriptables
{
    public class ProjectSo : ScriptableObject
    {
        #region Fields

        [SerializeField, HideInInspector]private string apiKey;
        [SerializeField, HideInInspector]private LongContainerSoDictionary containerSoDic;
        [SerializeField, HideInInspector]private Project projectInfo;
        [SerializeField, HideInInspector]private List<Container> containerList;
        [SerializeField, HideInInspector]private string lastUpdate = "";
        [SerializeField, HideInInspector]private List<LocaleField> projectLocales = new List<LocaleField>();
        
        private List<ContainerSo> _updatingContainerSos = new List<ContainerSo>();
        
        #endregion

        #region Getters

        public string ApiKey => apiKey;
        public Project ProjectInfo => projectInfo;
        public List<Container> ContainerList => containerList;
        public LongContainerSoDictionary ContainerSoDic => containerSoDic;
        public List<ContainerSo> UpdatingContainerSos => _updatingContainerSos;
        public string LastUpdate => lastUpdate;
        public List<LocaleField> ProjectLocales => projectLocales;
        
        #endregion

        #region Properties

        public bool IsFetchingUpdate { get; private set; } = false;
        public UpdateProjectOperation UpdateProjectOperationRef { get; private set; }

        #endregion

        #region Enums
        
        private enum ContainerUpdateType
        {
            None,
            SoftSync,
            HardSync,
            DataUpdated,
        }
        
        #endregion

        #region Events
        
        public event UnityAction OnBeginProjectUpdate;
        public event UnityAction OnFinishProjectUpdate;
        public event UnityAction<ContainerSo, RequestOperation> OnContainerSyncRequested;
        public event UnityAction OnAllContainerSyncFinished;
        
        #endregion

        #region Updates

        public bool IsSyncingContainerSos()
        {
            return _updatingContainerSos.Count > 0;
        }
        
        public void FetchUpdate()
        {
            if(IsFetchingUpdate) return;

            if (string.IsNullOrEmpty(apiKey))
            {
                Logger.LogError("Project API Key Is Missing", this);
                return;
            }
            IsFetchingUpdate = true; 
            OnBeginProjectUpdate?.Invoke();
            UpdateProjectOperationRef = new UpdateProjectOperation(this);
            UpdateProjectOperationRef.OnFinished += FinishFetch;
        }

        public void SyncContainersInfo(List<Container> containers)
        {
            containerList = containers;
            RemoveUnusedContainers();
            
            EditorUtility.SetDirty(this);
        }
        
        public void SyncProjectInfo(Project project)
        {
            projectInfo = project;
            
            if(projectInfo != null) UpdateLocales();
            
            EditorUtility.SetDirty(this);
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
                UpdateContainerSosData();
                lastUpdate = DateTime.Now.ToString(CultureInfo.CurrentCulture);
                EditorUtility.SetDirty(this);
            }

            UpdateProjectOperationRef = null;
            IsFetchingUpdate = false;
            AssetDatabase.SaveAssetIfDirty(this);
            OnFinishProjectUpdate?.Invoke();
        }
        
        #endregion

        #region ContainerSos Management
        
        private ContainerSo CreateContainerSo(Container containerInfo)
        {
            var containerSo = CreateInstance<ContainerSo>();
            containerSo.name = containerInfo.name;
            AssetDatabase.AddObjectToAsset(containerSo, this);
            containerSo.Init(containerInfo, this);
            return containerSo;
        }

        public void AddContainer(Container containerInfo)
        {
            if (projectInfo == null || containerList == null)
            {
                Logger.ProjectMissingInfo();
                return;
            }

            if (!containerSoDic.TryGetValue(containerInfo.id, out var containerSo))
            {
                containerSo = CreateContainerSo(containerInfo);
                containerSoDic.Add(containerInfo.id, containerSo);
                EditorUtility.SetDirty(this);
            }

            if (containerSo) return;
            
            containerSo = CreateContainerSo(containerInfo);
            containerSoDic[containerInfo.id] = containerSo;
            EditorUtility.SetDirty(this);
        }

        private void RemoveContainerSo(long containerId)
        {
            if (projectInfo == null || containerList == null)
            {
                Logger.ProjectMissingInfo();
                return;
            }

            if (!containerSoDic.TryGetValue(containerId, out var container)) return;

            if (container != null)
            {
                AssetDatabase.RemoveObjectFromAsset(container);    
            }
            
            containerSoDic.Remove(containerId);

            EditorUtility.SetDirty(this);
        }
        
        private void RemoveUnusedContainers()
        {
            if(containerList.Count <= 0) return;
            
            List<long> idsToRemove = new List<long>();
            
            foreach (long containerId in containerSoDic.Keys)
            {
                var containerFound = containerList.Find(container => containerId == container.id);
                if (containerFound == null)
                    idsToRemove.Add(containerId);
            }

            foreach (long id in idsToRemove)
            {
                RemoveContainerSo(id);
            }
        }

        public void RegisterContainerUpdate(ContainerSo containerSo, UpdateContainerOperation updateContainerOperation)
        {
            if(IsFetchingUpdate || _updatingContainerSos.Contains(containerSo)) return;
            
            _updatingContainerSos.Add(containerSo);
            
            updateContainerOperation.OnFinished += () =>
            {
                _updatingContainerSos.Remove(containerSo);
                if(_updatingContainerSos.Count <= 0) OnAllContainerSyncFinished?.Invoke();
            };
            OnContainerSyncRequested?.Invoke(containerSo, updateContainerOperation);
        }
        
        private void UpdateAllContainers(ContainerUpdateType updateType)
        {
            foreach (var container in containerList)
            {
                if (containerSoDic.TryGetValue(container.id, out var containerSo))
                {
                    if (!containerSo)
                    {
                        containerSo = CreateContainerSo(container);
                        containerSoDic[container.id] = containerSo;
                    }
                }
                else
                {
                    containerSo = CreateContainerSo(container);
                    containerSoDic.Add(container.id, containerSo);
                }
                
                UpdateContainerSo(containerSo, container, updateType);
            }
        }

        private void UpdateContainerSo(ContainerSo containerSo, Container containerInfo, ContainerUpdateType updateType)
        {
            switch (updateType)
            {
                case ContainerUpdateType.SoftSync:
                    containerSo.FetchEntries(false);
                    break;
                case ContainerUpdateType.HardSync:
                    containerSo.FetchEntries(true);
                    break;
                case ContainerUpdateType.DataUpdated:
                    containerSo.UpdateContainerInfo(containerInfo);
                    break;
                case ContainerUpdateType.None:
                    break;
            }
        }
        
        private void UpdateContainerSosData()
        {
            UpdateAllContainers(ContainerUpdateType.DataUpdated);
        }
        
        public void SyncContainerEntries(bool hardSync = false)
        {
            if (containerSoDic.Count <= 0)
            {
                Logger.LogWarning("No containers found to sync");
                return;
            }
            ContainerUpdateType updateType = hardSync ? ContainerUpdateType.HardSync : ContainerUpdateType.SoftSync;
            UpdateAllContainers(updateType);
        }
        
        #endregion
    }
}