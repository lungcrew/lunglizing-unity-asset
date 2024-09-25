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
    [CreateAssetMenu(fileName = "LungProject", menuName = "Lungfetcher/Lung Project", order = 1)]
    public class ProjectSo : ScriptableObject
    {
        #region Fields

        [SerializeField]private string apiKey;
        [SerializeField]private LongContainersSoDictionary projectContainerSoDic;
        [SerializeField]private Project projectInfo;
        [SerializeField]private List<Container> containerList;
        [SerializeField]private string lastUpdate = "";
        [SerializeField]private List<LocaleField> projectLocales = new List<LocaleField>();
        
        private List<ContainerSo> _updatingContainerSos = new List<ContainerSo>();
        
        #endregion

        #region Getters

        public string ApiKey => apiKey;
        public Project ProjectInfo => projectInfo;
        public List<Container> ContainerList => containerList;
        public LongContainersSoDictionary ProjectContainerSoDic => projectContainerSoDic;
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
            ProjectUpdated,
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

            IsFetchingUpdate = true; 
            OnBeginProjectUpdate?.Invoke();
            UpdateProjectOperationRef = new UpdateProjectOperation(this);
            UpdateProjectOperationRef.OnFinished += FinishFetch;
        }

        public void SyncContainersInfo(List<Container> containers)
        {
            containerList = containers;
            RemoveUnusedContainersFromDict();
            
            EditorUtility.SetDirty(this);
        }
        
        public void SyncProjectInfo(Project project)
        {
            projectInfo = project;
            
            if(projectInfo != null) UpdateLocales();
            
            EditorUtility.SetDirty(this);
        }

        private void RemoveUnusedContainersFromDict()
        {
            if(containerList.Count <= 0) return;
            
            List<long> idsToRemove = new List<long>();
            
            foreach (long containerId in projectContainerSoDic.Keys)
            {
                var containerFound = containerList.Find(container => containerId == container.id);
                if (containerFound == null)
                    idsToRemove.Add(containerId);
            }

            foreach (long id in idsToRemove)
            {
                projectContainerSoDic.Remove(id);
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
                UpdateContainerSosProjectData();
                lastUpdate = DateTime.Now.ToString(CultureInfo.CurrentCulture);
                EditorUtility.SetDirty(this);
            }

            UpdateProjectOperationRef = null;
            IsFetchingUpdate = false;
            OnFinishProjectUpdate?.Invoke();
        }
        
        #endregion

        #region ContainerSos Management

        public void AddContainerSo(ContainerSo containerSo, long containerId)
        {
            if (projectInfo == null || containerList == null)
            {
                Logger.ProjectMissingInfo();
                return;
            }
            
            if (!projectContainerSoDic.TryGetValue(containerId, out var containerSoList))
            {
                containerSoList = new ContainerSoList
                {
                    containerSos = new List<ContainerSo> { containerSo }
                };
                projectContainerSoDic.Add(containerId, containerSoList);
            }
            else
            {
                if (containerSoList.containerSos.Contains(containerSo))
                {
                    return;
                }

                containerSoList.containerSos.Add(containerSo);
            }
            
            EditorUtility.SetDirty(this);
        }

        public void SwitchContainerSoId(ContainerSo containerSo, long oldContainerId, long newContainerId)
        {
            if (projectInfo == null || containerList == null)
            {
                Logger.ProjectMissingInfo();
                return;
            }
            RemoveContainerSo(containerSo, oldContainerId);
            AddContainerSo(containerSo, newContainerId);
        }

        public void RemoveContainerSo(ContainerSo containerSo, long containerId)
        {
            if (projectInfo == null || containerList == null)
            {
                Logger.ProjectMissingInfo();
                return;
            }

            if (!projectContainerSoDic.TryGetValue(containerId, out var containerSoList)) return;

            if (!containerSoList.containerSos.Contains(containerSo)) return;
            
            containerSoList.containerSos.Remove(containerSo);
            if (containerSoList.containerSos.Count <= 0)
                projectContainerSoDic.Remove(containerId);

            EditorUtility.SetDirty(this);
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
        
        private void UpdateAllContainerSo(ContainerUpdateType updateType)
        {
            List<long> emptyKeys = new List<long>();
            
            foreach (var containerId in ProjectContainerSoDic.Keys)
            {
                var containerSoList = ProjectContainerSoDic[containerId].containerSos;
                
                for (int i = 0; i < containerSoList.Count;)
                {
                    var containerSo = containerSoList[i];
                    if (containerSo == null)
                    {
                        containerSoList.RemoveAt(i);
                        continue;
                    }
                    UpdateSingleContainerSo(containerSo, updateType);
                    i++;
                }
                
                if(containerSoList.Count <= 0) emptyKeys.Add(containerId);
            }

            foreach (var id in emptyKeys)
            {
                projectContainerSoDic.Remove(id);
            }
        }

        private void UpdateSingleContainerSo(ContainerSo containerSo,ContainerUpdateType updateType)
        {
            switch (updateType)
            {
                case ContainerUpdateType.SoftSync:
                    containerSo.FetchEntries(false);
                    break;
                case ContainerUpdateType.HardSync:
                    containerSo.FetchEntries(true);
                    break;
                case ContainerUpdateType.ProjectUpdated:
                    containerSo.ProjectUpdated();
                    break;
                case ContainerUpdateType.None:
                    break;
            }
        }

        private void ClearInvalidContainerSos()
        {
            UpdateAllContainerSo(ContainerUpdateType.None);
        }
        
        private void UpdateContainerSosProjectData()
        {
            UpdateAllContainerSo(ContainerUpdateType.ProjectUpdated);
        }
        
        public void SyncContainerSos(bool hardSync = false)
        {
            if (projectContainerSoDic.Count <= 0)
            {
                Logger.LogWarning("No containers found to sync");
                return;
            }
            ContainerUpdateType updateType = hardSync ? ContainerUpdateType.HardSync : ContainerUpdateType.SoftSync;
            UpdateAllContainerSo(updateType);
        }
        
        #endregion
    }
}