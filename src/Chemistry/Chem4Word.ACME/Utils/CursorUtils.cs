// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.IO;
using System.Windows.Input;

namespace Chem4Word.ACME.Utils
{
    public static class CursorUtils
    {
        //see https://stackoverflow.com/questions/38377743/how-to-customize-and-add-cursor-files-to-a-project

        public static Cursor Eraser { get; }
        public static Cursor Pencil { get; }

        static CursorUtils()
        {
            Eraser = FromByteArray(Properties.Resources.Eraser);
            Pencil = FromByteArray(Properties.Resources.Pencil);
        }

        public static Cursor FromByteArray(byte[] array)
        {
            using (MemoryStream memoryStream = new MemoryStream(array))
            {
                return new Cursor(memoryStream);
            }
        }
    }
}