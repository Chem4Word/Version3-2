// ---------------------------------------------------------------------------
//  Copyright (c) 2024, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Azure.Messaging.ServiceBus;
using System;

namespace Chem4Word.Helpers
{
    /// <summary>
    /// The sole purpose of this class is to keep references to assemblies which may only be used in supporting assemblies
    /// </summary>
    public class ReferenceKeeper : IDisposable
    {
        private ServiceBusClient ServiceBusClient { get; }

        private Guid _objectId;

        public ReferenceKeeper()
        {
            _objectId = Guid.NewGuid();
        }

        public void Dispose()
        {
            //
        }
    }
}