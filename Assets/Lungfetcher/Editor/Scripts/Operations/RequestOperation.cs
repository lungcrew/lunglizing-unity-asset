using System;
using System.Threading;

namespace Lungfetcher.Editor.Operations
{
    public class RequestOperation
    {

        #region Variables

        private bool _isFinished = false;
        private bool _isCanceled = false;
        protected bool isFinishedSuccessfully = false;
        protected float progress = 0;
        protected CancellationToken cancellationToken;
        private CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region Properties

        public bool IsCanceled => _isCanceled;
        public bool IsFinished => _isFinished;
        public float Progress => progress;
        public bool IsFinishedSuccessfully => isFinishedSuccessfully;

        #endregion

        #region Events

        public event Action OnFinished;
        public event Action<float> OnProgressUpdated;

        #endregion


        #region Operation Methods

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

        #endregion
    }
}