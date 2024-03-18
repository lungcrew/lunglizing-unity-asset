
using Lungfetcher.Data;
using UnityEngine;

namespace Lungfetcher.Helper
{
    public class DebugLog
    {
        public static void ProjectNotFound(long projectId)
        {
            Debug.LogError($"{projectId} Not Found");
        }

        public static void ProjectReferenceMissing(long projectId)
        {
            Debug.LogError($"{projectId} Reference Missing");
        }

        public static void TableNotFound(long tableId, long projectId)
        {
            Debug.LogError($"{tableId} Not Found in {projectId}");
        }
        
        public static void TableReferenceMissing(long tableId, long projectId)
        {
            Debug.LogError($"{tableId} Reference Missing in {projectId}");
        }

        public static void EntryNotFound(long entryId, long tableId, long projectId)
        {
            Debug.LogError($"{entryId} Not Found in {tableId} in {projectId}");
        }
        
        public static void EntryReferenceMissing(long entryId, long tableId, long projectId)
        {
            Debug.LogError($"{entryId} Reference Missing in {tableId} in {projectId}");
        }
    }
}