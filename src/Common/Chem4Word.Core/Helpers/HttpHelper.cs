// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;

namespace Chem4Word.Core.Helpers
{
    public static class HttpHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static ApiResult InvokeGet(string apiUrl, Dictionary<string, string> extraHeaders = null)
        {
            ApiResult result = new ApiResult();

            SetConnectionLeaseTimeout(apiUrl);
            SetServicePointManagerProperties();
            SetCommonHeaders();
            AddExtraHeaders(extraHeaders);

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, apiUrl))
            {
                try
                {
                    HttpResponseMessage response = _httpClient.SendAsync(request).Result;

                    result.StatusCode = response.StatusCode;
                    result.Content = response.Content.ReadAsStringAsync().Result;
                    result.Message = $"{response.ReasonPhrase}";
                }
                catch (HttpRequestException httpRequestException)
                {
                    result.StatusCode = HttpStatusCode.InternalServerError;
                    result.Message = httpRequestException.Message;
                    Debugger.Break();
                    Console.WriteLine($"Request error: {httpRequestException.Message}");
                }
                catch (Exception exception)
                {
                    result.StatusCode = HttpStatusCode.InternalServerError;
                    result.Message = exception.Message;
                    Debugger.Break();
                    Console.WriteLine($"Unexpected error: {exception.Message}");
                }
            }

            return result;
        }

        private static void SetServicePointManagerProperties()
        {
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        private static void SetConnectionLeaseTimeout(string uri)
        {
            // http://byterot.blogspot.com/2016/07/singleton-httpclient-dns.html
            ServicePoint sp = ServicePointManager.FindServicePoint(new Uri(uri));
            sp.ConnectionLeaseTimeout = 60 * 1000; // 1 minute
        }

        private static void SetCommonHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("user-agent", "Chem4Word");
        }

        private static void AddExtraHeaders(Dictionary<string, string> extraHeaders)
        {
            if (extraHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in extraHeaders)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
        }
    }
}
