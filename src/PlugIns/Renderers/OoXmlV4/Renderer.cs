// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Chem4Word.Core;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Renderer.OoXmlV4.OOXML;
using IChem4Word.Contracts;

namespace Chem4Word.Renderer.OoXmlV4
{
    public class Renderer : IChem4WordRenderer
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public string Name => "Open Office Xml Renderer V4";
        public string Description => "This is the standard renderer for Chem4Word 2020";
        public bool HasSettings => true;

        public Point TopLeft { get; set; }
        public IChem4WordTelemetry Telemetry { get; set; }
        public string SettingsPath { get; set; }

        public string Cml { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        private OoXmlV4Options _rendererOptions;

        public Renderer()
        {
            // Nothing to do here
        }

        public bool ChangeSettings(Point topLeft)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                Telemetry.Write(module, "Verbose", "Called");
                _rendererOptions = new OoXmlV4Options(SettingsPath);

                OoXmlV4Settings settings = new OoXmlV4Settings();
                settings.Telemetry = Telemetry;
                settings.TopLeft = topLeft;

                OoXmlV4Options tempOptions = _rendererOptions.Clone();
                settings.SettingsPath = SettingsPath;
                settings.RendererOptions = tempOptions;

                DialogResult dr = settings.ShowDialog();
                if (dr == DialogResult.OK)
                {
                    _rendererOptions = tempOptions.Clone();
                }
                settings.Close();
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }

            return true;
        }

        public string Render()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            string result = null;

            try
            {
                Telemetry.Write(module, "Verbose", "Called");
                _rendererOptions = new OoXmlV4Options(SettingsPath);

                string guid = Properties["Guid"];
                result = OoXmlFile.CreateFromCml(Cml, guid, _rendererOptions, Telemetry, TopLeft);
                if (!File.Exists(result))
                {
                    Telemetry.Write(module, "Exception", "Structure could not be rendered.");
                    UserInteractions.WarnUser("Sorry this structure could not be rendered.");
                }

                // Deliberate crash to test Error Reporting
                //int ii = 2;
                //int dd = 0;
                //int bang = ii / dd;
            }
            catch (Exception ex)
            {
                new ReportError(Telemetry, TopLeft, module, ex).ShowDialog();
            }

            return result;
        }
    }
}