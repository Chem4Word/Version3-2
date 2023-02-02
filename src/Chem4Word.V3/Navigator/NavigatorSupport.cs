// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Microsoft.Office.Interop.Word;
using Microsoft.Office.Tools;

namespace Chem4Word.Navigator
{
    public static class NavigatorSupport
    {
        public static void SelectNavigatorItem(string guid)
        {
            CustomTaskPane custTaskPane = null;
            foreach (CustomTaskPane taskPane in Globals.Chem4WordV3.CustomTaskPanes)
            {
                Application app = Globals.Chem4WordV3.Application;
                if (app.ActiveWindow == taskPane.Window && taskPane.Title == Constants.NavigatorTaskPaneTitle)
                {
                    custTaskPane = taskPane;
                }

                if (custTaskPane != null
                    && custTaskPane.Control is NavigatorHost navHost)
                {
                    var navigatorControl = navHost.navigatorView1;
                    if (navigatorControl.DataContext is NavigatorController controller)
                    {
                        int idx = 0;
                        foreach (var item in controller.NavigatorItems)
                        {
                            if (item.CustomControlTag.Equals(guid))
                            {
                                navigatorControl.NavigatorList.SelectedIndex = idx;
                                navigatorControl.NavigatorList.ScrollIntoView(navigatorControl.NavigatorList.SelectedItem);
                                break;
                            }
                            idx++;
                        }
                    }
                }
            }
        }
    }
}