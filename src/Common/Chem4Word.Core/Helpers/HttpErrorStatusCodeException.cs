// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Net;
using System.Net.Http;

namespace Chem4Word.Core.Helpers
{
    public class HttpErrorStatusCodeException : HttpRequestException
    {
        public HttpErrorStatusCodeException(HttpStatusCode errorStatusCode)
        {
            ErrorStatusCode = errorStatusCode;
        }
        public HttpStatusCode ErrorStatusCode { get; set; }
    }
}
