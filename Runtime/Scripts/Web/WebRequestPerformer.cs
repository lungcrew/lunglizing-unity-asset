using System.Threading.Tasks;

namespace Lungfetcher.Web
{
    public delegate void Started(WebRequest request);
    public delegate void Stopped(WebResponse response);

    public static class EM_WebRequestPerfomer
    {
        #region Events

        public static event Started OnRequestStarted;
        public static event Stopped OnResponseError;

        #endregion

        #region Requesting

        public static async Task<WebResponse> Perform(WebRequest request)
        {
            OnRequestStarted?.Invoke(request);
            WebResponse response = await request.SendAsync();

            if (!response.Success)
            {
                OnResponseError?.Invoke(response);
            }

            return response;
        }

        #endregion
    }
}