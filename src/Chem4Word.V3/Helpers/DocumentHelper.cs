// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Microsoft.Office.Interop.Word;

namespace Chem4Word.Helpers
{
    public static class DocumentHelper
    {
        /// <summary>
        /// Finds the active document in the Documents collection and returns it.
        /// This is required because Application.ActiveDocument Object can be changed by Word, while functions are executing.
        /// </summary>
        /// <returns></returns>
        public static Document GetActiveDocument()
        {
            Document activeDocument = null;

            try
            {
                var application = Globals.Chem4WordV3.Application;
                var currentDocumentName = Globals.Chem4WordV3.CurrentDocumentName;

                foreach (Document document in application.Documents)
                {
                    if (document.Name.Equals(currentDocumentName))
                    {
                        activeDocument = document;
                    }
                }
            }
            catch
            {
                // Do Nothing
            }

            return activeDocument;
        }
    }
}