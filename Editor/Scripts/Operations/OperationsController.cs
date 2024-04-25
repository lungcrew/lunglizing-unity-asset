using System.Collections.Generic;
using Lungfetcher.Data;

namespace Lungfetcher.Editor.Operations
{
    public static class OperationsController
    {
        private static Dictionary<long, FetchOperation<List<EntriesLocale>>> _entriesLocaleFetchDic = new Dictionary<long, FetchOperation<List<EntriesLocale>>>();
        private static Dictionary<string, FetchOperation<List<Table>>> _projectTablesFetchDic = new Dictionary<string, FetchOperation<List<Table>>>();
        private static Dictionary<string, FetchOperation<Project>> _projectInfoFetchDic = new Dictionary<string, FetchOperation<Project>>();
        
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
        
        public static FetchOperation<List<Table>> RequestFetchProjectTables(string endpoint, string accessKey)
        {
            bool contains = _projectTablesFetchDic.TryGetValue(accessKey, out var fetchOperation);
            
            if (contains)
            {
                return fetchOperation;
            }

            fetchOperation = new FetchOperation<List<Table>>();
            
            _projectTablesFetchDic.Add(accessKey, fetchOperation);
            fetchOperation.OnFinished += () => _projectTablesFetchDic.Remove(accessKey);
            fetchOperation.Fetch(endpoint, accessKey);

            return fetchOperation;
        }

        public static FetchOperation<List<EntriesLocale>> RequestFetchTableEntries(long tableId, string accessKey)
        {
            bool contains = _entriesLocaleFetchDic.TryGetValue(tableId, out var fetchOperation);

            if (contains)
            {
                return fetchOperation;
            }
            
            fetchOperation = new FetchOperation<List<EntriesLocale>>();
            _entriesLocaleFetchDic.Add(tableId, fetchOperation);
            fetchOperation.OnFinished += () => _entriesLocaleFetchDic.Remove(tableId);
            fetchOperation.Fetch("tables/" + tableId + "/localized-entries", accessKey);
            
            return fetchOperation;
        }
    }
}