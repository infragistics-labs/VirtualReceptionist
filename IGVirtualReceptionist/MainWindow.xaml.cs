using IGVirtualReceptionist.Interfaces;
using IGVirtualReceptionist.Models;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace IGVirtualReceptionist
{
    public enum ReceptionistViewState
    {
        MainVideo,
        WaveToStart,
        MainView,
        Directory,
        CallState
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IReceptionistView
    {
        private KinectModel kinectModel = null;
        private ReceptionistViewState viewState = ReceptionistViewState.MainVideo;
        public event PropertyChangedEventHandler PropertyChanged;
        public ReceptionistViewState CurrentViewState
        {
            get
            {
                return viewState;
            }
            set
            {
                viewState = value;
                OnPropertyChanged("CurrentViewState");
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            kinectModel = KinectModel.Instance;
            kinectModel.StartedTrackingNewBodies += KinectModel_StartedTrackingNewBodies;
            kinectModel.StoppedTrackingBodies += KinectModel_StoppedTrackingBodies;
        }

        private void KinectModel_StoppedTrackingBodies(object sender, BodyTrackingEventArgs e)
        {
            tcMain.SelectedItem = TabVideo;
        }

        private void KinectModel_StartedTrackingNewBodies(object sender, BodyTrackingEventArgs e)
        {
            tcMain.SelectedItem = TabMain;
        }

        public void GestureCaptured(GestureEventArgs args)
        {
            
        }

        private Grammar baseGrammar;
        public Grammar GetGrammar()
        {
            if (baseGrammar == null)
            {
                using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(Properties.Resources.BaseGrammar)))
                {
                    baseGrammar = new Grammar(memoryStream);
                }
            }
            return baseGrammar;
        }

        public void SpeechRecognitionRejected(SpeechRecognitionRejectedEventArgs e)
        {
            
        }

        public void SpeechRecognized(SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence >= 0.9f && e.Result.Semantics.Value.ToString() == "BACK")
            {
                Dispatcher.BeginInvoke(new Action(GoBack));
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            kinectModel.RegisterActiveView(this);

            this.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;

            if (SystemParameters.VirtualScreenWidth < 2000)
            {
                this.Width = 1600;
                this.Height = 600;
            }
            else
            {
                this.Width = SystemParameters.VirtualScreenWidth;
                this.Height = SystemParameters.VirtualScreenHeight;
            }

            this.Left= System.Windows.Forms.Screen.AllScreens.Min(s => s.Bounds.Left);
            this.Top = System.Windows.Forms.Screen.AllScreens.Min(s => s.Bounds.Top);
        }

        private void GoBack()
        {

            if (this.tcMain.SelectedIndex == 0)
                return;

            switch(this.tcMain.SelectedIndex)
            {
                case 1:
                    {
                        IGVirtualReceptionist.Views.ClippyView clippyView = LogicalTreeHelper.FindLogicalNode(Window.GetWindow(this), "Avatar") as Views.ClippyView;
                        if (clippyView == null || clippyView.IsFullScreen)
                            this.tcMain.SelectedIndex = 0;
                        else
                            clippyView.IsFullScreen = true;
                    }
                    break;
                case 2:
                    {
                        this.tcMain.SelectedIndex = 1;
                        this.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            IGVirtualReceptionist.Views.ClippyView clippyView = LogicalTreeHelper.FindLogicalNode(Window.GetWindow(this), "Avatar") as Views.ClippyView;
                            if (clippyView != null)
                                clippyView.IsFullScreen = false;
                        }));
                    }
                    break;

            }
        }

    }
}
