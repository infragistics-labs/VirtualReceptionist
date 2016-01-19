using System.Windows;
using System.Windows.Controls;
using IGVirtualReceptionist;

namespace IGVirtualReceptionist.Views
{
    public partial class MainVideoView : ViewBase
    {
         

        #region Constructor
        public MainVideoView()
        {
            InitializeComponent();
        }
        #endregion // Constructor

        #region Events

        #region mePromoVideo_MediaFailed
        void mePromoVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Unable to play Promo Video!");
        }
        #endregion // mePromoVideo_MediaFailed

        #region mePromoVideo_MediaEnded
        private void mePromoVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            this.RestartVideo();
        }
        #endregion // mePromoVideo_MediaEnded

        #region mePromoVideo_Loaded
        private void mePromoVideo_Loaded(object sender, RoutedEventArgs e)
        {
            this.StartVideo();
        }
        #endregion // mePromoVideo_Loaded

        #endregion // Events

        #region Methods

        #region StartVideo
        public void StartVideo()
        {
            mePromoVideo.Play();
        }
        #endregion // StartVideo

        #region PauseVideo
        public bool PauseVideo()
        {
            if (mePromoVideo.CanPause)
            {
                mePromoVideo.Pause();
                return true;
            }
            return false;
        }
        #endregion // PauseVideo

        #region RestartVideo
        public void RestartVideo()
        {
            this.StopVideo();
            this.StartVideo();
        }
        #endregion // RestartVideo

        #region StopVideo
        public void StopVideo()
        {
            mePromoVideo.Stop();
        }
        #endregion // StopVideo

        #endregion // Methods

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TabControl tab = (TabControl)App.Current.MainWindow.FindName("tcMain");
            tab.SelectedIndex = 1;
        }
    }
}
