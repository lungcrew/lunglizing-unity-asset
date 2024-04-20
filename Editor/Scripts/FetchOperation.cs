using System.Threading.Tasks;
using Lungfetcher.Web;
using UnityEngine.Events;

namespace Lungfetcher.Editor
{
    public class FetchOperation<T> : RequestOperation where T : class
    {
        public event UnityAction<T> OnResponse;
        
        public T ResponseData { get; private set; }
        public string AccessKey { get; private set; }

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