// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------
using Chem4Word.Model2;

namespace Chem4Word.ACME
{
    public interface IHostedWpfEditor
    {
        bool IsDirty { get; }
        Model EditedModel { get; }
    }
}