using System.Collections.Generic;
using Lungfetcher.Data;

namespace Lungfetcher.Editor.Operations
{
    public static class OperationsController
    {
        #region Dictionaries

        private static Dictionary<long, FetchOperation<List<EntriesLocale>>> _entriesLocaleFetchDic = new Dictionary<long, FetchOperation<List<EntriesLocale>>>();
        private static Dictionary<string, FetchOperation<List<Container>>> _projectContainersFetchDic = new Dictionary<string, FetchOperation<List<Container>>>();
        private static Dictionary<string, FetchOperation<Project>> _projectInfoFetchDic = new Dictionary<string, FetchOperation<Project>>();

        #endregion

        #region Fetch Requests

        public static FetchOperation<Project> RequestFetchProjectInfo(string endpoint, string accessKey)
        {
            bool contains = _projectInfoFetchDic.TryGetValue(accessKey, out var fetchOperation);
            
            if (contains)
            {
                return fetchOperation;
            }

            fetchOperation = new FetchOperation<Project>();
            
            _projectInfoFetchDic.Add(accessKey, fetchOperation);
            fetchOperation.OnFinished += () => _projectInfoFetchDic.Remove(accessKey);
                
            fetchOperation.Fetch(endpoint, accessKey);
            
            return fetchOperation;
        }
        
        public static FetchOperation<List<Container>> RequestFetchProjectContainers(string endpoint, string accessKey)
        {
            bool contains = _projectContainersFetchDic.TryGetValue(accessKey, out var fetchOperation);
            
            if (contains)
            {
                return fetchOperation;
            }

            fetchOperation = new FetchOperation<List<Container>>();
            
            _projectContainersFetchDic.Add(accessKey, fetchOperation);
            fetchOperation.OnFinished += () => _projectContainersFetchDic.Remove(accessKey);
            fetchOperation.Fetch(endpoint, accessKey);

            return fetchOperation;
        }

        public static FetchOperation<List<EntriesLocale>> RequestFetchContainersEntries(long containerId, string accessKey)
        {
            bool contains = _entriesLocaleFetchDic.TryGetValue(containerId, out var fetchOperation);

            if (contains)
            {
                return fetchOperation;
            }
            
            fetchOperation = new FetchOperation<List<EntriesLocale>>();
            _entriesLocaleFetchDic.Add(containerId, fetchOperation);
            fetchOperation.OnFinished += () => _entriesLocaleFetchDic.Remove(containerId);
            fetchOperation.Fetch("containers/" + containerId + "/localized-locales", accessKey);
            
            return fetchOperation;
        }

        #endregion
    }
}