// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Rolls up Bond Stereo and Order into a single class to facilitate binding.
    /// Deals with combinations of Orders and Stereo
    /// </summary>
    ///

    public class ReactionOption : DependencyObject
    {
        public int Id
        {
            get { return (int)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        public static readonly DependencyProperty IdProperty =
            DependencyProperty.Register("Id", typeof(int), typeof(ReactionOption), new PropertyMetadata(default(int)));

        public Globals.ReactionType ReactionType
        {
            get { return (Globals.ReactionType)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register("Type", typeof(Globals.ReactionType), typeof(ReactionOption),
                                        new PropertyMetadata(Globals.ReactionType.Normal));

        public System.Windows.Media.Drawing ReactionGraphic
        {
            get { return (System.Windows.Media.Drawing)GetValue(ReactionGraphicProperty); }
            set { SetValue(ReactionGraphicProperty, value); }
        }

        public static readonly DependencyProperty ReactionGraphicProperty =
            DependencyProperty.Register("ReactionGraphic", typeof(System.Windows.Media.Drawing), typeof(ReactionOption),
                                        new PropertyMetadata(default(System.Windows.Media.Drawing)));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(ReactionOption),
                                        new PropertyMetadata(default(string)));

        public string Description
        {
            get
            {
                switch (ReactionType)
                {
                    case Globals.ReactionType.Normal:
                        {
                            return "Normal reaction";
                        }

                    case Globals.ReactionType.Reversible:
                        {
                            return "Equilibrium Reaction";
                        }

                    case Globals.ReactionType.ReversibleBiasedForward:
                        {
                            return "Forward-biased equilibrium";
                        }
                    case Globals.ReactionType.ReversibleBiasedReverse:
                        {
                            return "Reverse-biasedequilibrium";
                        }
                    case Globals.ReactionType.Blocked:
                        {
                            return "Blocked reaction";
                        }

                    default:
                        return "";
                }
            }
        }

        public override string ToString()
        {
            return $"{ReactionType}";
        }
    }
}