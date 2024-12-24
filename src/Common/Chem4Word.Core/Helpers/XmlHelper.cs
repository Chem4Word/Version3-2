// ---------------------------------------------------------------------------
//  Copyright (c) 2025, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;

namespace Chem4Word.Core.Helpers
{
    public static class XmlHelper
    {
        /// <summary>
        /// Adds XML header if required
        /// </summary>
        /// <param name="cml"></param>
        /// <returns></returns>
        public static string AddHeader(string cml)
        {
            var header = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
            var result = cml;

            if (!cml.StartsWith(header))
            {
                if (cml.Contains(Environment.NewLine))
                {
                    result = $"{header}{Environment.NewLine}{cml}";
                }
                else
                {
                    result = $"{header}{cml}";
                }
            }

            return result;
        }
    }
}