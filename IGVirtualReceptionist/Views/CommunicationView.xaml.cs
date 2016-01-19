using IGVirtualReceptionist.Interfaces;
using IGVirtualReceptionist.Models;
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IGVirtualReceptionist.Views
{
    /// <summary>
    /// Interaction logic for Welcome.xaml
    /// </summary>
    public partial class CommunicationView : ViewBase
    {
        #region Members

        private DirectoryModel directory;
        private string userId;
        int timeoutInSeconds;
        bool timedOut = false;
        private Timer timer;

        #endregion //Members

        #region CommunicationView

        public CommunicationView(string id, int timeoutInSeconds)
        {
            InitializeComponent();

            this.directory = new DirectoryModel();
            this.timeoutInSeconds = timeoutInSeconds;

            this.InitializeUI(id);
        }

        #endregion //CommunicationView

        #region Events

        public event EventHandler CommunicationEnded;
        protected void OnCommunicationEnded()
        {
            if (CommunicationEnded != null)
                this.Dispatcher.Invoke((Action)(() =>
            {
                this.CommunicationEnded(this, EventArgs.Empty);
            }));
        }

        public event EventHandler VideoCallStarted;
        protected void OnVideoCallStarted()
        {
            if (VideoCallStarted != null)
                this.Dispatcher.Invoke((Action)(() =>
                {
                    this.VideoCallStarted(this, EventArgs.Empty);
                }));
        }

        #endregion //Events

        #region Properties

        #region TimedOut

        internal bool TimedOut
        {
            get
            { 
                return timedOut;
            }
        }

        #endregion //TimedOut

        #endregion //Properties

        #region Methods

        #region GetHandle

        private IntPtr GetHandle()
        {
            HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;

            IntPtr handle = IntPtr.Zero;
            if (hwndSource != null)
            {
                handle = hwndSource.Handle;
            }
            return handle;
        }

        #endregion //GetHandle

        #region GetVideoBounds()

        private Rect GetVideoBounds()
        {
            Point relativePoint = this.VideoPane.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
            return new Rect(relativePoint.X, relativePoint.Y, this.ConfirmationContainer.ActualWidth, this.ConfirmationContainer.ActualHeight);
        }

        #endregion //GetVideoBouds

        #region InitializeUI

        internal void InitializeUI(string id)
        {
            this.timedOut = false; 
            this.userId = id;
            //look up the contact information and change label

            this.Dispatcher.Invoke((Action)(() =>
            {
                this.VideoPane.Visibility = System.Windows.Visibility.Collapsed;
                this.ConfirmationContainer.Visibility = System.Windows.Visibility.Visible;
                DirectoryEntryModel employee = directory.GetEntryByEmail(this.userId);
                this.ConfirmationTextBlock.Text = string.Format("Would you like to initiate a video call with {0}?", employee.FullName);
            }));
        }

        #endregion //InitializeUI

        #endregion //Methods

        #region Event Handlers

        #region Yes_Btn_Click

        private void Yes_Btn_Click(object sender, RoutedEventArgs e)
        {
            if (timer == null)
            {
                timer = new Timer(this.timeoutInSeconds * 1000);
                timer.Elapsed += (p1, p2) =>
                    {
                        this.timedOut = true;
                        timer.Stop();
                        IGVirtualReceptionist.Communication.CommunicationManager.HangUpVideoCall(this.userId);
                    };
            }

            this.ConfirmationContainer.Visibility = System.Windows.Visibility.Hidden;
            this.VideoPane.Visibility = System.Windows.Visibility.Visible;

            Rect bounds = this.GetVideoBounds();

            IGVirtualReceptionist.Communication.CommunicationManager.InitiateVideoCall(
                this.GetHandle(),
                (int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height,
                this.userId,
                (ar) =>
                {
                    this.OnCommunicationEnded();
                },
                (ar) =>
                {
                    this.timer.Stop();
                });

            this.timer.Start();
        }

        #endregion //Yes_Btn_Click

        #region No_Btn_Click

        private void No_Btn_Click(object sender, RoutedEventArgs e)
        {
            this.OnCommunicationEnded();
        }

        #endregion //No_Btn_Click

        #endregion //Event Handlers

    }
}
