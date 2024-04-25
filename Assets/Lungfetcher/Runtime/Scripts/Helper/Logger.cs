using UnityEngine;

namespace Lungfetcher.Helper
{
    public static class Logger
    {
        private static string Prefix => $"<color=#FFFFFF>[Lungfetcher]</color>";
        
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="sender">The object that triggered the error (optional).</param>
        public static void Log(string message, UnityEngine.Object sender = null)
        {
#if UNITY_EDITOR
            Debug.Log($"{Prefix} {message}", sender);
#endif
        }

        /// <summary>
        /// Logs an warning message.
        /// </summary>
        /// <param name="message">The warning message.</param>
        /// <param name="sender">The object that triggered the error (optional).</param>
        public static void LogWarning(string message, UnityEngine.Object sender = null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{Prefix} {message}", sender);
#endif
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="sender">The object that triggered the error (optional).</param>
        public static void LogError(string message, UnityEngine.Object sender = null)
        {
#if UNITY_EDITOR
            Debug.LogError($"{Prefix} {message}", sender);
#endif
        }
        
        public static void ProjectMissingInfo(UnityEngine.Object sender = null)
        {
            LogError("Project Missing Info, try syncing it first", sender);
        }
    }
}