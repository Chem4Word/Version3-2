// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Chem4Word.ACME.Models;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Helpers;
using Chem4Word.Model2.Converters.CML;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Word;
using ContentControl = Microsoft.Office.Interop.Word.ContentControl;

namespace Chem4Word.Navigator
{
    public class NavigatorController
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        public ObservableCollection<ChemistryObject> NavigatorItems { get; }

        //references the custom XML parts in the document
        private CustomXMLParts Parts { get; }

        //local reference to the active document
        private readonly Document _doc;

        public NavigatorController()
        {
            NavigatorItems = new ObservableCollection<ChemistryObject>();
        }

        public NavigatorController(Document doc) : this()
        {
            //get a reference to the document
            _doc = doc;
            Parts = _doc.CustomXMLParts.SelectByNamespace(CMLNamespaces.cml.NamespaceName);
            Parts.PartAfterLoad += OnPartAfterLoad;
            Parts.PartBeforeDelete += OnPartBeforeDelete;

            LoadModel();
        }

        /// <summary>
        /// Loads up the model initially from the document
        /// </summary>
        private void LoadModel()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            try
            {
                var converter = new CMLConverter();
                if (NavigatorItems.Any())
                {
                    NavigatorItems.Clear();
                }
                if (_doc != null)
                {
                    var added = new Dictionary<string, int>();

                    var navItems = from ContentControl ccs in _doc.ContentControls
                                   join CustomXMLPart part in Parts
                                     on CustomXmlPartHelper.GuidFromTag(ccs?.Tag) equals CustomXmlPartHelper.GetCmlId(part)
                                   orderby ccs.Range.Start
                                   let chemModel = converter.Import(part.XML)
                                   select new ChemistryObject
                                   {
                                       CustomControlTag = CustomXmlPartHelper.GuidFromTag(ccs?.Tag),
                                       Cml = part.XML,
                                       Formula = chemModel.ConciseFormula
                                   };

                    foreach (var chemistryObject in navItems)
                    {
                        if (!string.IsNullOrEmpty(chemistryObject.CustomControlTag)
                            && !added.ContainsKey(chemistryObject.CustomControlTag))
                        {
                            NavigatorItems.Add(chemistryObject);
                            added.Add(chemistryObject.CustomControlTag, 1);
                        }
                    }

                    Debug.WriteLine("Number of items loaded = {0}", NavigatorItems.Count);
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        /// <summary>
        /// handles deletion of an XML Part...removes the corresponding navigator item
        /// </summary>
        /// <param name="OldPart">The custom XML part that gets deleted</param>
        private void OnPartBeforeDelete(CustomXMLPart OldPart)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var oldPart = NavigatorItems.FirstOrDefault(ni
                                                                => CustomXmlPartHelper.GuidFromTag(ni.CustomControlTag) == CustomXmlPartHelper.GetCmlId(OldPart));
                NavigatorItems.Remove(oldPart);
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Occurs after a new custom XMl part is loaded into the document
        /// Useful for updating the Navigator
        /// </summary>
        /// <param name="NewPart"></param>
        private void OnPartAfterLoad(CustomXMLPart NewPart)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            try
            {
                var converter = new CMLConverter();
                //get the chemistry
                var chemModel = converter.Import(NewPart.XML);
                //find out which content control matches the custom XML part
                try
                {
                    // ReSharper disable once InconsistentNaming
                    var matchingCC = (from ContentControl cc in _doc.ContentControls
                                      orderby cc.Range.Start
                                      where CustomXmlPartHelper.GuidFromTag(cc.Tag) == CustomXmlPartHelper.GetCmlId(NewPart)
                                      select cc).First();

                    //get the ordinal position of the content control
                    int start = 0;
                    foreach (ContentControl cc in _doc.ContentControls)
                    {
                        if (cc.ID == matchingCC.ID)
                        {
                            break;
                        }
                        start += 1;
                    }

                    //insert the new navigator item at the ordinal position
                    var newNavItem = new ChemistryObject
                    {
                        CustomControlTag = matchingCC?.Tag,
                        Cml = NewPart.XML,
                        Formula = chemModel.ConciseFormula
                    };
                    try
                    {
                        NavigatorItems.Insert(start, newNavItem);
                    }
                    catch (ArgumentOutOfRangeException) //can happen when there are more content controls than navigator items
                    {
                        //so simply insert the new navigator item at the end
                        NavigatorItems.Add(newNavItem);
                    }
                }
                catch (InvalidOperationException)
                {
                    //sequence contains no elements - thrown on close
                    //just ignore
                }
            }
            catch (Exception ex)
            {
                using (var form = new ReportError(Globals.Chem4WordV3.Telemetry, Globals.Chem4WordV3.WordTopLeft, module, ex))
                {
                    form.ShowDialog();
                }
            }
        }
    }
}