// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Net;

namespace Chem4Word.Core.Helpers
{
    public class ApiResult
    {
        public HttpStatusCode StatusCode { get; set; }

        public string Message { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }
}
