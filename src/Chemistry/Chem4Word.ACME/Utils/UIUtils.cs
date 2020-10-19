// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Models;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using IChem4Word.Contracts;
using Application = System.Windows.Application;

namespace Chem4Word.ACME.Utils
{
    public static class UIUtils
    {
        public static bool? ShowDialog(Window dialog, object parent)
        {
            HwndSource source = (HwndSource)HwndSource.FromVisual((Visual)parent);
            if (source != null)
            {
                new WindowInteropHelper(dialog).Owner = source.Handle;
            }
            return dialog.ShowDialog();
        }

        public static void ShowAcmeSettings(EditorCanvas currentEditor, AcmeOptions options, IChem4WordTelemetry telemetry, Point topLeft)
        {
            var mode = Application.Current.ShutdownMode;
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var pe = new SettingsHost(options, telemetry, topLeft);
            ShowDialog(pe, currentEditor);
            Application.Current.ShutdownMode = mode;
        }

        public static Point GetOffScreenPoint()
        {
            int maxX = Int32.MinValue;
            int maxY = Int32.MinValue;

            foreach (var screen in Screen.AllScreens)
            {
                maxX = Math.Max(maxX, screen.Bounds.Right);
                maxY = Math.Max(maxY, screen.Bounds.Bottom);
            }

            return new Point(maxX + 100, maxY + 100);
        }

        public static Point GetOnScreenPoint(Point target, double width, double height)
        {
            double left = target.X - width / 2;
            double top = target.Y - height / 2;

            foreach (var screen in Screen.AllScreens)
            {
                if (screen.Bounds.Contains((int)target.X, (int)target.Y))
                {
                    // Checks are done in this order to ensure the title bar is always accessible

                    // Handle too far right
                    if (left + width > screen.WorkingArea.Right)
                    {
                        left = screen.WorkingArea.Right - width;
                    }

                    // Handle too low
                    if (top + height > screen.WorkingArea.Bottom)
                    {
                        top = screen.WorkingArea.Bottom - height;
                    }

                    // Handle too far left
                    if (left < screen.WorkingArea.Left)
                    {
                        left = screen.WorkingArea.Left;
                    }

                    // Handle too high
                    if (top < screen.WorkingArea.Top)
                    {
                        top = screen.WorkingArea.Top;
                    }
                }
            }

            return new Point(left, top);
        }

        public static void DoPropertyEdit(MouseButtonEventArgs e, EditorCanvas currentEditor)
        {
            EditViewModel evm = (EditViewModel)currentEditor.ViewModel;

            var position = e.GetPosition(currentEditor);
            var screenPosition = currentEditor.PointToScreen(position);

            // Did RightClick occur on a Molecule Selection Adorner?
            var moleculeAdorner = currentEditor.GetMoleculeAdorner(position);
            if (moleculeAdorner != null)
            {
                if (moleculeAdorner.AdornedMolecules.Count == 1)
                {
                    screenPosition = GetDpiAwareScaledPosition(screenPosition, moleculeAdorner);

                    var mode = Application.Current.ShutdownMode;
                    Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                    var model = new MoleculePropertiesModel();
                    model.Centre = screenPosition;
                    model.Path = moleculeAdorner.AdornedMolecules[0].Path;
                    model.Used1DProperties = evm.Used1DProperties;

                    model.Data = new Model();
                    Molecule mol = moleculeAdorner.AdornedMolecules[0].Copy();
                    model.Data.AddMolecule(mol);
                    mol.Parent = model.Data;

                    model.Charge = mol.FormalCharge;
                    model.Count = mol.Count;
                    model.SpinMultiplicity = mol.SpinMultiplicity;
                    model.ShowMoleculeBrackets = mol.ShowMoleculeBrackets;

                    var pe = new MoleculePropertyEditor(model, evm.EditorOptions);
                    ShowDialog(pe, currentEditor);

                    if (model.Save)
                    {
                        var thisMolecule = model.Data.Molecules.First().Value;
                        evm.UpdateMolecule(moleculeAdorner.AdornedMolecules[0], thisMolecule);
                    }

                    Application.Current.ShutdownMode = mode;
                }
            }
            else
            {
                // Did RightClick occur on a ChemicalVisual?
                var activeVisual = currentEditor.GetTargetedVisual(position);
                if (activeVisual != null)
                {
                    screenPosition = GetDpiAwareScaledPosition(screenPosition, activeVisual);

                    // Did RightClick occur on an AtomVisual?
                    if (activeVisual is AtomVisual av)
                    {
                        var mode = Application.Current.ShutdownMode;

                        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                        var atom = av.ParentAtom;
                        var model = new AtomPropertiesModel
                        {
                            Centre = screenPosition,
                            Path = atom.Path,
                            Element = atom.Element
                        };

                        if (atom.Element is Element)
                        {
                            model.IsFunctionalGroup = false;
                            model.IsElement = true;

                            model.Charge = atom.FormalCharge ?? 0;
                            model.Isotope = atom.IsotopeNumber.ToString();
                            model.ExplicitC = atom.ExplicitC;
                        }

                        if (atom.Element is FunctionalGroup)
                        {
                            model.IsElement = false;
                            model.IsFunctionalGroup = true;
                        }

                        model.MicroModel = new Model();

                        Molecule m = new Molecule();
                        model.MicroModel.AddMolecule(m);
                        m.Parent = model.MicroModel;

                        Atom a = new Atom();
                        a.Element = atom.Element;
                        a.Position = atom.Position;
                        a.FormalCharge = atom.FormalCharge;
                        a.IsotopeNumber = atom.IsotopeNumber;
                        m.AddAtom(a);
                        a.Parent = m;

                        foreach (var bond in atom.Bonds)
                        {
                            Atom ac = new Atom();
                            ac.Element = Globals.PeriodicTable.C;
                            ac.ExplicitC = false;
                            ac.Position = bond.OtherAtom(atom).Position;
                            m.AddAtom(ac);
                            ac.Parent = m;
                            Bond b = new Bond(a, ac);
                            b.Order = bond.Order;
                            if (bond.Stereo != Globals.BondStereo.None)
                            {
                                b.Stereo = bond.Stereo;
                                if (bond.Stereo == Globals.BondStereo.Wedge || bond.Stereo == Globals.BondStereo.Hatch)
                                {
                                    if (atom.Path.Equals(bond.StartAtom.Path))
                                    {
                                        b.StartAtomInternalId = a.InternalId;
                                        b.EndAtomInternalId = ac.InternalId;
                                    }
                                    else
                                    {
                                        b.StartAtomInternalId = ac.InternalId;
                                        b.EndAtomInternalId = a.InternalId;
                                    }
                                }
                            }
                            m.AddBond(b);
                            b.Parent = m;
                        }
                        model.MicroModel.ScaleToAverageBondLength(20);

                        var pe = new AtomPropertyEditor(model, evm.EditorOptions);

                        ShowDialog(pe, currentEditor);
                        Application.Current.ShutdownMode = mode;

                        if (model.Save)
                        {
                            evm.UpdateAtom(atom, model);

                            evm.ClearSelection();
                            evm.AddToSelection(atom);

                            if (model.AddedElement != null)
                            {
                                AddOptionIfNeeded(model);
                            }
                            evm.SelectedElement = model.Element;
                        }
                        pe.Close();
                    }

                    // Did RightClick occur on a BondVisual?
                    if (activeVisual is BondVisual bv)
                    {
                        var mode = Application.Current.ShutdownMode;

                        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                        var bond = bv.ParentBond;

                        var model = new BondPropertiesModel
                        {
                            Centre = screenPosition,
                            Path = bond.Path,
                            Angle = bond.Angle,
                            Length = bond.BondLength / Globals.ScaleFactorForXaml,
                            BondOrderValue = bond.OrderValue.Value,
                            IsSingle = bond.Order.Equals(Globals.OrderSingle),
                            IsDouble = bond.Order.Equals(Globals.OrderDouble),
                            Is1Point5 = bond.Order.Equals(Globals.OrderPartial12),
                            Is2Point5 = bond.Order.Equals(Globals.OrderPartial23)
                        };

                        model.BondAngle = model.AngleString;
                        model.DoubleBondChoice = DoubleBondType.Auto;

                        if (model.IsDouble || model.Is1Point5 || model.Is2Point5)
                        {
                            if (bond.ExplicitPlacement != null)
                            {
                                model.DoubleBondChoice = (DoubleBondType)bond.ExplicitPlacement.Value;
                            }
                            else
                            {
                                if (model.IsDouble && bond.Stereo == Globals.BondStereo.Indeterminate)
                                {
                                    model.DoubleBondChoice = DoubleBondType.Indeterminate;
                                }
                            }
                        }

                        if (model.IsSingle)
                        {
                            model.SingleBondChoice = SingleBondType.None;

                            switch (bond.Stereo)
                            {
                                case Globals.BondStereo.Wedge:
                                    model.SingleBondChoice = SingleBondType.Wedge;
                                    break;

                                case Globals.BondStereo.Hatch:
                                    model.SingleBondChoice = SingleBondType.Hatch;
                                    break;

                                case Globals.BondStereo.Indeterminate:
                                    model.SingleBondChoice = SingleBondType.Indeterminate;
                                    break;

                                default:
                                    model.SingleBondChoice = SingleBondType.None;
                                    break;
                            }
                        }

                        model.ClearFlags();

                        var pe = new BondPropertyEditor(model);
                        ShowDialog(pe, currentEditor);
                        Application.Current.ShutdownMode = mode;

                        if (model.Save)
                        {
                            evm.UpdateBond(bond, model);
                            evm.ClearSelection();

                            bond.Order = Globals.OrderValueToOrder(model.BondOrderValue);
                            evm.AddToSelection(bond);
                        }
                    }
                }
            }

            void AddOptionIfNeeded(AtomPropertiesModel model)
            {
                if (!evm.AtomOptions.Any(ao => ao.Element.Symbol == model.AddedElement.Symbol))
                {
                    AtomOption newOption = null;
                    switch (model.AddedElement)
                    {
                        case Element elem:
                            newOption = new AtomOption(elem);
                            break;

                        case FunctionalGroup group:
                            newOption = new AtomOption(group);
                            break;
                    }
                    evm.AtomOptions.Add(newOption);
                }
            }
        }

        private static Point GetDpiAwareScaledPosition(Point screenPosition, Visual visual)
        {
            Point pp = screenPosition;

            PresentationSource source = PresentationSource.FromVisual(visual);
            if (source != null && source.CompositionTarget != null)
            {
                double dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                double dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;

                pp = new Point(pp.X * 96.0 / dpiX, pp.Y * 96.0 / dpiY);
            }

            return pp;
        }
    }
}