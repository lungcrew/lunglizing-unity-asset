using System;
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
        protected bool isFinishedSuccessfully = false;
        protected float progress = 0;
        protected CancellationToken cancellationToken;
        private CancellationTokenSource _cancellationTokenSource;
        
        public bool IsCanceled => _isCanceled;
        public bool IsFinished => _isFinished;
        public float Progress => progress;
        public bool IsFinishedSuccessfully => isFinishedSuccessfully;
        
        public void CancelOperation()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _isCanceled = true;
        }

        protected void FinishOperation(bool success)
        {
            if (success)
            {
                progress = 100;
            }

            _isCanceled = cancellationToken.IsCancellationRequested;
            _cancellationTokenSource?.Dispose();
            _isFinished = true;
            isFinishedSuccessfully = success;
            OnFinished?.Invoke();
        }
        
        protected void GenerateCancellationToken()
        {
            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = _cancellationTokenSource.Token;
        }
    }
}