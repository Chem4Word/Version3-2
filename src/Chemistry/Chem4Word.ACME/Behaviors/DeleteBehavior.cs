// ---------------------------------------------------------------------------
//  Copyright (c) 2022, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Input;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing.Visuals;
using Chem4Word.ACME.Utils;

namespace Chem4Word.ACME.Behaviors
{
    public class DeleteBehavior : BaseEditBehavior
    {
        private Window _parent;

        private Cursor _cursor;

        public override void Abort()
        {
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            _parent = Application.Current.MainWindow;
            CurrentEditor = (EditorCanvas)AssociatedObject;

            _cursor = CurrentEditor.Cursor;
            CurrentEditor.Cursor = CursorUtils.Eraser;
            CurrentEditor.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            //set up an intercept on mouse move to control adorner visibility
            CurrentEditor.PreviewMouseMove += CurrentEditor_PreviewMouseMove;
            CurrentEditor.IsHitTestVisible = true;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            }
            //clear the current selection
            EditController.ClearSelection();
            CurrentStatus = "Click to remove an atom or bond.";
        }

        private void CurrentEditor_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            //needed to detect when we are over a hydrogen visual
            var hitTestResult = CurrentEditor.GetTargetedVisual(e.GetPosition(CurrentEditor));
            if (hitTestResult is HydrogenVisual hv)
            {
                //turn off the H adorner display
                e.Handled = true;
                //and set the active visual to the parent
                CurrentEditor.ActiveVisual = hv.ParentVisual;
            }
        }

        private void CurrentEditor_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var hitTestResult = CurrentEditor.ActiveVisual;
            switch (hitTestResult)
            {
                case HydrogenVisual _:
                    //bail out - we shouldn't be deleting implicit Hs
                    return;

                case GroupVisual gv:
                    {
                        var parent = gv.ParentMolecule;
                        EditController.DeleteMolecule(parent);
                        CurrentStatus = "Group deleted";
                        break;
                    }

                case AtomVisual atomVisual:
                    {
                        var atom = atomVisual.ParentAtom;
                        EditController.DeleteAtoms(new[] { atom });
                        CurrentStatus = "Atom deleted.";
                        break;
                    }

                case BondVisual bondVisual:
                    {
                        var bond = bondVisual.ParentBond;
                        EditController.DeleteBonds(new[] { bond });
                        CurrentStatus = "Bond deleted";
                        break;
                    }

                case ReactionVisual reactionVisual:
                    {
                        var reaction = reactionVisual.ParentReaction;
                        EditController.DeleteReactions(new[] { reaction });
                        CurrentStatus = "Bond deleted";
                        break;
                    }
                case AnnotationVisual annotationVisual:
                    {
                        var annotation = annotationVisual.ParentAnnotation;
                        EditController.DeleteAnnotations(new[] { annotation });
                        CurrentStatus = "Annotation deleted.";
                        break;
                    }
            }

            EditController.ClearSelection();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (CurrentEditor != null)
            {
                CurrentEditor.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
                CurrentEditor.PreviewMouseMove -= CurrentEditor_PreviewMouseMove;
                CurrentEditor.IsHitTestVisible = false;
                CurrentEditor.Cursor = _cursor;
                CurrentStatus = "";
            }
        }
    }
}