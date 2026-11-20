using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snake.Core
{
    public abstract class BaseUrlManager
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BaseUrlManager(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        protected IHttpContextAccessor HttpContextAccessor
        {
            get { return _httpContextAccessor; }
        }

        protected abstract string ControllerName { get; }

        protected Uri GetBaseUrl()
        {
            var currentContext = _httpContextAccessor.HttpContext;

            var sp = new StringBuilder();

            if (currentContext == null)
            {
                return new Uri(string.Empty);
            }

            var request = currentContext.Request;

            if (request == null)
            {
                return new Uri(string.Empty);
            }

            var host = request.Host.ToUriComponent();
            var pathBase = request.PathBase.ToUriComponent();
            sp.Append($"{request.Scheme}://{host}{pathBase}");

            if (!pathBase.EndsWith(UrlsHelper.CONST_Slash))
            {
                sp.Append(UrlsHelper.CONST_Slash);
            }

            return new Uri(sp.ToString());
        }

        protected Uri GetUrl(string url)
        {
            UriBuilder uriBuilder = new UriBuilder(GetBaseUrl());
            uriBuilder.Path += url;
            return uriBuilder.Uri;
        }

        protected Uri GetActionUrl(string actionUrl)
        {
            UriBuilder uriBuilder = new UriBuilder(GetBaseUrl());
            uriBuilder.Path += CreateActionUrl(actionUrl);
            return uriBuilder.Uri;
        }

        protected Uri AddQueryString(string url, IDictionary<string, string> dictionary)
        {
            if (dictionary.Count == 0)
            {
                return new Uri(url);
            }

            var dic = dictionary
                .Where(x => !string.IsNullOrEmpty(x.Value))
                .ToDictionary(p => p.Key, p => p.Value);
            return new Uri(QueryHelpers.AddQueryString(url, dic));
        }
        
        protected string MergeParams(params string[] list)
        {
            var sb = new StringBuilder();

            foreach (var param in list)
            {
                sb.Append(param);
            }

            return sb.ToString();
        }

        private string CreateActionUrl(string actionUrl)
        {
            var sp = new StringBuilder();

            sp.Append(ControllerName);
            sp.Append(UrlsHelper.CONST_Slash);
            sp.Append(actionUrl);
            return sp.ToString();
        }
    }
}
