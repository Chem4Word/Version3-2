// ---------------------------------------------------------------------------
//  Copyright (c) 2021, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;
using System.Windows;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Commands
{
    public class PasteCommand : BaseCommand
    {
        public PasteCommand(EditViewModel vm) : base(vm)
        {
        }

        public override bool CanExecute(object parameter)
        {
            var canExecute = Clipboard.ContainsData(Globals.FormatCML) || Clipboard.ContainsData(Globals.FormatSDFile) || Clipboard.ContainsText();
            return canExecute;
        }

        public override void Execute(object parameter)
        {
            CMLConverter cmlConverter = new CMLConverter();
            SdFileConverter sdfConverter = new SdFileConverter();

            if (Clipboard.ContainsData(Globals.FormatCML))
            {
                string pastedCML = (string)Clipboard.GetData(Globals.FormatCML);
                EditViewModel.PasteCML(pastedCML);
            }
            else if (Clipboard.ContainsText())
            {
                bool failedCML = false;
                bool failedSDF = false;
                string pastedText = Clipboard.GetText();
                Model buffer = null;
                //try to convert the pasted text with the CML converter first
                try
                {
                    buffer = cmlConverter.Import(pastedText);
                }
                catch
                {
                    failedCML = true;
                }

                if (failedCML)
                {
                    buffer = sdfConverter.Import(pastedText);
                    failedSDF = buffer.GeneralErrors.Any();
                }

                if (failedCML & failedSDF)
                {
                    if (buffer.GeneralErrors.Any())
                    {
                        Chem4Word.Core.UserInteractions.InformUser("Unable to paste text as chemistry: " + buffer.GeneralErrors[0]);
                    }
                    else
                    {
                        Chem4Word.Core.UserInteractions.InformUser("Unable to paste text as chemistry: unknown error.");
                    }
                }
                else
                {
                    EditViewModel.PasteModel(buffer);
                }
            }
        }
    }
}