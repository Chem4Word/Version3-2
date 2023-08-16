// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Interfaces;
using System;
using System.Security.Cryptography;

namespace Chem4Word.ACME.Models
{
    public class CryptoRandomizer : IRandomizer
    {
        int IRandomizer.RandomInt(int max)
        {
            // Create a byte array to hold the random value.
            var byteArray = new byte[4];

            using (var gen = new RNGCryptoServiceProvider())
            {
                gen.GetBytes(byteArray);
                return Math.Abs(BitConverter.ToInt32(byteArray, 0) % max);
            }
        }

        int IRandomizer.RandomInt(int min, int max)
        {
            if (max == min) return 0;
            // Create a byte array to hold the random value.
            var byteArray = new byte[4];

            using (var gen = new RNGCryptoServiceProvider())
            {
                gen.GetBytes(byteArray);
                return Math.Abs(BitConverter.ToInt32(byteArray, 0) % (max - min) + min);
            }
        }
    }
}