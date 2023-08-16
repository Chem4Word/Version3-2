// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System.Windows;

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

        public ReactionType ReactionType
        {
            get { return (ReactionType)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register("Type", typeof(ReactionType), typeof(ReactionOption),
                                        new PropertyMetadata(ReactionType.Normal));

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
                    case ReactionType.Normal:
                        {
                            return "Normal reaction";
                        }

                    case ReactionType.Reversible:
                        {
                            return "Equilibrium Reaction";
                        }

                    case ReactionType.ReversibleBiasedForward:
                        {
                            return "Forward-biased equilibrium";
                        }

                    case ReactionType.ReversibleBiasedReverse:
                        {
                            return "Reverse-biased equilibrium";
                        }

                    case ReactionType.Blocked:
                        {
                            return "Blocked reaction";
                        }

                    case ReactionType.Retrosynthetic:
                        {
                            return "Retrosynthesis step";
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