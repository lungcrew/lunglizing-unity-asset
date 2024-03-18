using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lungfetcher.Data;
using Lungfetcher.Editor.Scriptables;
using Lungfetcher.Web;
using UnityEngine;
using UnityEngine.Events;

namespace Lungfetcher.Editor
{
    public class RequestOperation
    {
        public event UnityAction OnFinished;
        private bool _isFinished = false;
        private bool _isCanceled = false;
        private float _progress = 0;
        private CancellationToken _cancellationToken;
        private CancellationTokenSource _cancellationTokenSource;
        public bool IsFinished => _isFinished;
        public float Progress => _progress;
        
        public async void UpdateProject(ProjectSo projectSo)
        {
            GenerateCancellationToken();
            LungRequest projInfoReq = LungRequest.Create("info", projectSo.ApiKey);
            LungRequest projTablesReq = LungRequest.Create("tables", projectSo.ApiKey);

            var tasks = new List<Task>();
            var projInfoTask = projInfoReq.Fetch<Project>(_cancellationTokenSource.Token);
            var projTablesTask = projTablesReq.Fetch<List<Table>>(_cancellationTokenSource.Token);
            tasks.Add(projInfoTask);
            tasks.Add(projTablesTask);
            
            await Task.WhenAll(tasks);

            if (_cancellationToken.IsCancellationRequested)
            {
                FinishOperation(false);
                return;
            }

            if (!projectSo)
            {
                Debug.LogError("Request Failed: Project Scriptable is missing");
                FinishOperation(false);
                return;
            }
            
            var projInfoResponse = projInfoTask.Result;
            var projTablesResponse = projTablesTask.Result;
            
            if (projInfoResponse != null)
            {
                projectSo.SyncProjectInfo(projInfoResponse.data);
            }
            else
            {
                Debug.LogError("Project info request failed");
            }
            
            if (projTablesResponse != null)
                projectSo.SyncTables(projTablesResponse.data);
            else
            {
                Debug.LogError("Project tables request failed");
            }
            
            FinishOperation(true);
        }
        
        public void CancelOperation()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _isCanceled = true;
        }

        private void FinishOperation(bool success)
        {
            if (success)
            {
                _progress = 100;
            }

            
            _isCanceled = _cancellationToken.IsCancellationRequested;
            _cancellationTokenSource?.Dispose();
            _isFinished = true;
            OnFinished?.Invoke();
        }
        
        private void GenerateCancellationToken()
        {
            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
        }
    }
}