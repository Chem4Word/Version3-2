// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Microsoft.Office.Interop.Word;

namespace Chem4Word.Helpers
{
    public class WordSettings
    {
        public bool CorrectSentenceCaps { get; }
        public bool SmartCutPaste { get; }

        public WordSettings(Application application)
        {
            CorrectSentenceCaps = application.AutoCorrect.CorrectSentenceCaps;
            application.AutoCorrect.CorrectSentenceCaps = false;

            SmartCutPaste = application.Options.SmartCutPaste;
            application.Options.SmartCutPaste = false;
        }

        public void RestoreSettings(Application application)
        {
            application.AutoCorrect.CorrectSentenceCaps = CorrectSentenceCaps;
            application.Options.SmartCutPaste = SmartCutPaste;
        }
    }
}