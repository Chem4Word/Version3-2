// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace IChem4Word.Contracts
{
    public interface IChem4WordTelemetry
    {
        void Write(string source, LoggingLevel level, string message);

        void SendZipFile(byte[] bytes, string fileName);
    }
}
