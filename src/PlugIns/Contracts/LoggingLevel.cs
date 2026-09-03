// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.ComponentModel;

namespace IChem4Word.Contracts
{
    public enum LoggingLevel
    {
        // Please keep this list in alphabetical order

        [Description("Action")]
        Action,

        [Description("AutomaticUpdate")]
        AutomaticUpdate,

        [Description("Data (CML)")]
        DataCml,

        [Description("Data (MDL)")]
        DataMol,

        [Description("Data (SketchEl!)")]
        DataSketch,

        [Description("Debug")]
        Debug,

        [Description("Error")]
        Error,

        [Description("Exception")]
        Exception,

        [Description("Exception (CML)")]
        ExceptionCml,

        [Description("Exception (Data)")]
        ExceptionData,

        [Description("Exception (MDL)")]
        ExceptionMol,

        [Description("Data (SketchEl!)")]
        ExceptionSketch,

        [Description("Information")]
        Information,

        [Description("Integrity")]
        Integrity,

        [Description("Msi Actions")]
        MsiActions,

        [Description("Read Manual")]
        ReadManual,

        [Description("Setup")]
        Setup,

        [Description("Search")]
        Search,

        [Description("Search Failure")]
        SearchFailure,

        [Description("Search Failure (Data)")]
        SearchFailureData,

        [Description("Search Warning (Data)")]
        SearchWarningData,

        [Description("Exception (StackTrace)")]
        StackTrace,

        [Description("Timing")]
        Timing,

        [Description("Update")]
        Update,

        [Description("Verbose")]
        Verbose,

        [Description("Warning")]
        Warning,

        [Description("Warning (CML)")]
        WarningCml,

        [Description("Warning (Data)")]
        WarningData,
    }
}
