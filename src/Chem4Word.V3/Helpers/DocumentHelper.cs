// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
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
        /// This is required because Application.ActiveDocument can be changed by Word functions are executing.
        /// </summary>
        /// <returns></returns>
        public static Document GetActiveDocument()
        {
            Document activeDocument = null;

            var application = Globals.Chem4WordV3.Application;

            var documentName = application.ActiveDocument.Name;

            foreach (Document document in application.Documents)
            {
                if (document.Name.Equals(documentName))
                {
                    activeDocument = document;
                }
            }

            return activeDocument;
        }
    }
}