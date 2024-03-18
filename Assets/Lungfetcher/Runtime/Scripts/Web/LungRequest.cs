using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Lungfetcher.Development;
using UnityEngine;

namespace Lungfetcher.Web
{
    public class LungRequest
    {
        #region Static

        private static readonly string LocalBaseURL = "http://localhost/v0/external";
        private static readonly string ProductionBaseURL = "https://api.lunglizing.com/v0/external";
        private static string BaseURL => ResolveURL();

        private static string ResolveURL()
        {
            DevelopmentAnchor anchor = Resources.Load<DevelopmentAnchor>("DevelopmentAnchor");
            return anchor != null && anchor.DevelopmentOn ? LocalBaseURL : ProductionBaseURL;
        }

        public static LungRequest Create(string endpoint, string accessKey)
        {
            LungRequest request = new(endpoint);
            request.SetAccessKey(accessKey);
            return request;
        }

        #endregion

        #region Fields

        private WebRequest _request;

        #endregion

        #region Constructors

        private LungRequest(string endpoint)
        {
            _request = WebRequest.To($"{BaseURL}/{endpoint}");
        }

        #endregion

        #region Preparation

        public LungRequest SetAccessKey(string key)
        {
            _request.SetBearerAuth(key);
            return this;
        }

        #endregion

        #region Fetching

        public async Task<LungResponse<T>> Fetch<T>(CancellationToken token = default) where T : class
        {
            try
            {
                WebResponse response = await _request.SendAsync(token);
            
                if (!response.Success)
                {
                    return default;
                }

                return response.GetBodyAs<LungResponse<T>>();
            }
            catch (OperationCanceledException)
            {
                return default;
            }
        }

        #endregion
    }
}