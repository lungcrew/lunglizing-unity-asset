using System.Collections.Generic;
using Lungfetcher.Editor.Helper;
using Lungfetcher.Editor.Scriptables;
using UnityEngine;

namespace Lungfetcher.Editor
{
    public static class OperationsController
    {
        private static LongTableRequestDictionary _tableRequestDic = new LongTableRequestDictionary();
        private static LongProjectRequestDictionary _projectRequestsDic = new LongProjectRequestDictionary();
        private static ProjectUpdateRequestDictionary _projectUpdateRequestDic = new ProjectUpdateRequestDictionary();

        public static RequestOperation RequestProjectUpdate(ProjectSo projectSo)
        {
            if (_projectUpdateRequestDic.ContainsKey(projectSo))
            {
                return null;
            }

            var requestOperation = new RequestOperation();
            
            _projectUpdateRequestDic.Add(projectSo, requestOperation);
            
            requestOperation.OnFinished += () => RemoveProjectUpdateRequest(projectSo);
            
            requestOperation.UpdateProject(projectSo);
            
            return requestOperation;
        }
        
        public static RequestOperation RequestTableEntries(long tableId, long projectId)
        {
            if (_tableRequestDic.ContainsKey(tableId))
            {
                Debug.LogError("Request already in progress");
                return null;
            }
            
            var requestOperation = new RequestOperation();
            
            _tableRequestDic.Add(tableId, requestOperation);
            AddRequestToProject(projectId, requestOperation);
            
            requestOperation.OnFinished += () => RemoveRequestFromProject(projectId, requestOperation);
            requestOperation.OnFinished += () => RemoveTableRequest(tableId);

            return requestOperation;
        }

        public static void RemoveProjectUpdateRequest(ProjectSo projectSo)
        {
            if (!ContainsProjectUpdateRequest(projectSo))
            {
                return;
            }
            
            var requestOperation = _projectUpdateRequestDic[projectSo];
            
            if(!requestOperation.IsFinished)
                requestOperation.CancelOperation();
            
            _projectUpdateRequestDic.Remove(projectSo);
        }
        
        public static void RemoveTableRequest(long tableId)
        {
            var requestOperation = GetTableRequest(tableId);
            
            if (requestOperation == null)
            {
                return;
            }
            
            if(!requestOperation.IsFinished)
                requestOperation.CancelOperation();
            
            _tableRequestDic.Remove(tableId);
        }
        

        private static void AddRequestToProject(long projectId, RequestOperation requestOperation)
        {
            if (ContainsProjectRequests(projectId))
            {
                _projectRequestsDic[projectId].Add(requestOperation);
            }
            else
            {
                _projectRequestsDic.Add(projectId, new List<RequestOperation>(){requestOperation});
            }
        }

        private static void RemoveRequestFromProject(long projectId, RequestOperation requestOperation)
        {
            var containProject = _projectRequestsDic.TryGetValue(projectId, out var requestOperationList);
            
            if (!containProject)
            {
                return;
            }
            
            requestOperationList.Remove(requestOperation);
            
            if(!requestOperation.IsFinished)
                requestOperation.CancelOperation();
            
            if (requestOperationList.Count <= 0)
                _projectRequestsDic.Remove(projectId);
        }

        public static RequestOperation GetProjectUpdateRequest(ProjectSo projectSo)
        {
            bool containsRequest = _projectUpdateRequestDic.TryGetValue(projectSo, out var request);
            return containsRequest ? request : null;
        }
        
        public static RequestOperation GetTableRequest(long tableId)
        {
            bool containsRequest = _tableRequestDic.TryGetValue(tableId, out var request);
            return containsRequest ? request : null;
        }
        
        public static List<RequestOperation> GetProjectRequests(long projectId)
        {
            bool containsRequests = _projectRequestsDic.TryGetValue(projectId, out var requestList);
            return containsRequests ? requestList : null;
        }

        public static bool ContainsProjectUpdateRequest(ProjectSo projectSo)
        {
            return _projectUpdateRequestDic.ContainsKey(projectSo);
        }

        public static bool ContainsTableRequest(long tableId)
        {
            return _tableRequestDic.ContainsKey(tableId);
        }

        public static bool ContainsProjectRequests(long projectId)
        {
            return _projectRequestsDic.ContainsKey(projectId);
        }
    }
}