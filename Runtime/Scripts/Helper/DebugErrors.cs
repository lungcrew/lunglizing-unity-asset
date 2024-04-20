using UnityEngine;

namespace Lungfetcher.Helper
{
    public static class DebugErrors
    {
        public static void ProjectMissingInfo()
        {
            Debug.LogError("Project Missing Info, try syncing it first");
        }
        public static void ProjectInfoFailed(string apikey)
        {
            Debug.LogError($"failed to get project info: ApiKey {apikey}");
        }

        public static void ProjectFetchTablesFailed(string apikey)
        {
            Debug.LogError($"failed to fetch project tables: ApiKey {apikey}");
        }

        public static void TableIdNotFound(long tableId, long projectId)
        {
            Debug.LogError($"Table id: {tableId} Not found in project: {projectId}");
        }

        public static void TableSoNotFound(long tableId, long projectId)
        {
            Debug.LogError($"Table Scriptable Not Found in id: {tableId} of project: {projectId}");
        }
    }
}