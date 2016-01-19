using IGVirtualReceptionist.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IGVirtualReceptionist
{
    internal static class Utilities
    {
        #region IsDesignMode

        internal static bool IsDesignMode
        {
            get
            {
                // Check for design mode. 
                return ((bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue));
            }
        }

        #endregion //IsDesignMode

        #region MakeVideoCall

        internal static void MakeVideoCall(string primaryId, UIElementCollection uiElementCollection, int timeoutInSeconds, string secondaryId = null)
        {
            CommunicationView commView = new CommunicationView(primaryId, timeoutInSeconds);
            uiElementCollection.Add(commView);
            commView.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            commView.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            commView.Width = 700;
            commView.Height = 400;
            commView.CommunicationEnded += (p1, p2) =>
            {
                if (commView.TimedOut && 
                    string.IsNullOrEmpty(secondaryId)==false)
                {
                    commView.InitializeUI(secondaryId);
                    secondaryId = null;
                    return;
                }
                else
                {
                    commView.Visibility = Visibility.Collapsed;
                uiElementCollection.Remove(commView);
                }
            };
        }

        #endregion //MakeVideoCall
    }
}
