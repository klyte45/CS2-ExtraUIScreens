using Belzont.Utils;
using cohtml;
using cohtml.Net;
using System.Collections;
using System.Collections.Generic;
using DefaultResourceHandler = Colossal.UI.DefaultResourceHandler;

namespace ExtraUIScreens
{
    public class EuisResourceHandler : DefaultResourceHandler
    {
        public CohtmlUISystem System { get; set; }

        public string liveViewData => $"[{string.Join(",", (typeof(UserImagesManager).GetField("m_LiveViews", ReflectionUtils.allFlags).GetValue(System.UserImagesManager) as Dictionary<string, CohtmlLiveView>)?.Keys)}]";
        public override void OnResourceRequest(IResourceRequest request, IResourceResponse response)
        {
            LogUtils.DoLog("EuisResourceHandler.OnResourceRequest {0} - LiveViews = {1}", request.GetURL(), liveViewData);
            if (IsLiveViewRequest(request.GetURL()))
            {
                ResourceRequestData requestData = new(request.GetId(), request.GetURL(), response);
                coroutineHost.StartCoroutine(TryPreloadedTextureRequestAsync(requestData));
            }
            else
            {
                base.OnResourceRequest(request, response);
            }
        }

        private void RequestLiveViewResource(ResourceRequestData requestData)
        {
            ResourceResponse.UserImageData userImageData = this.System.UserImagesManager.CreateLiveViewImageData(requestData.UriBuilder.Uri.AbsoluteUri);
            if (userImageData != null)
            {
                requestData.Response.ReceiveUserImage(userImageData);
                requestData.RespondWithSuccess();
                return;
            }
            requestData.RespondWithFailure(string.Format("Cannot find live View Url: {0}", requestData.UriBuilder.Uri));
        }

        private bool IsLiveViewRequest(string uri) => System.UserImagesManager.ContainsLiveView(uri);
        private IEnumerator TryPreloadedTextureRequestAsync(ResourceRequestData requestData)
        {
            RequestLiveViewResource(requestData);
            yield break;
        }
    }
}
