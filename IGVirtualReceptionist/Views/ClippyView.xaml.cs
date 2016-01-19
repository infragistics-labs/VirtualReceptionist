using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IGVirtualReceptionist.Views
{
    /// <summary>
    /// Interaction logic for ucClippy.xaml
    /// </summary>
    public partial class ClippyView : ViewBase
    {
        public ClippyView()
        {
            InitializeComponent();

            
        }


        #region Properties

        #region IsExpanded

        // Dependency Property
        public static readonly DependencyProperty IsFullScreenProperty =
             DependencyProperty.Register("IsFullScreen", typeof(Boolean),
             typeof(ClippyView), new FrameworkPropertyMetadata(true));

        // .NET Property wrapper
        public bool IsFullScreen
        {
            get { return (bool)GetValue(IsFullScreenProperty); }
            set { SetValue(IsFullScreenProperty, value); }
        }

        #endregion //IsExpanded

        #endregion //Properties


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.ToggleState();
        }

        private void ToggleState()
        {
            this.IsFullScreen = !this.IsFullScreen;
            if (IsFullScreen)
            {
                lblClippyText.Content = "Wave to Start!";
            }
            else
            {
                lblClippyText.Content = "Raise hand or speak command!";
            }
        }

        public override void GestureCaptured(Models.GestureEventArgs args)
        {
            if (args.Name.Equals("Wave"))
            {
                if (args.Confidence > 0.2)
                {
                    this.IsFullScreen = false;
                    lblClippyText.Content = "Raise hand or speak command!";
                }
            }
        }
    }
}
