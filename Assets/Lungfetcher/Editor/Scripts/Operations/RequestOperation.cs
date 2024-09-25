using System.Threading;
using UnityEngine.Events;

namespace Lungfetcher.Editor.Operations
{
    public class RequestOperation
    {
        public event UnityAction OnFinished;
        public event UnityAction<float> OnProgressUpdated;
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
                UpdateProgress(100f);
            }

            _isCanceled = cancellationToken.IsCancellationRequested;
            _cancellationTokenSource?.Dispose();
            _isFinished = true;
            isFinishedSuccessfully = success;
            OnFinished?.Invoke();
        }

        protected void UpdateProgress(float newProgress)
        {
            progress = newProgress;
            OnProgressUpdated?.Invoke(progress);
        }
        
        protected void GenerateCancellationToken()
        {
            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = _cancellationTokenSource.Token;
        }
    }
}