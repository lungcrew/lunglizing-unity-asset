using System;
using Lungfetcher.Web;

namespace Lungfetcher.Editor.Operations
{
    public class FetchOperation<T> : RequestOperation where T : class
    { 
        public T ResponseData { get; private set; }
        public string AccessKey { get; private set; }
        
        public event Action<T> OnResponse;

        public async void Fetch(string endpoint, string accessKey)
        {
            AccessKey = accessKey;
            GenerateCancellationToken();
            LungRequest request = LungRequest.Create(endpoint, accessKey);
            
            var response = await request.Fetch<T>(cancellationToken);
            
            ResponseData = response?.data;
            OnResponse?.Invoke(ResponseData);

            FinishOperation(ResponseData != null);
        }
    }
}