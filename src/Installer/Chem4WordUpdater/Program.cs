// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Chem4WordUpdater
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Version osVer = Environment.OSVersion.Version;

            if (!(osVer.Major >= 10))
            {
                // Running Windows 10 or 11
                MessageBox.Show("You must be running Windows 10 or 11", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            else if (args.Length == 1)
            {
                bool created;
                using (new Mutex(true, "e02309d1-6734-4b66-a31d-76439a9ee978", out created))
                {
                    if (created)
                    {
                        Version version = Assembly.GetExecutingAssembly().GetName().Version;

                        RegistryHelper.WriteAction($"Starting Updater V{version}");

                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        Application.Run(new Updater(args));
                    }
                    else
                    {
                        RegistryHelper.WriteAction("Updater is already running");
                    }
                }
            }
            else
            {
                MessageBox.Show("Please use 'Check for Updates' on Chemistry Ribbon (in Word)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }
    }
}