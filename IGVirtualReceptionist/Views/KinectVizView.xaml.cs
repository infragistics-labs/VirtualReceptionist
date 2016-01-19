using IGVirtualReceptionist.Models;
using Microsoft.Kinect;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IGVirtualReceptionist.Views
{

    /// <summary>
    /// Interaction logic for ucKinectViz.xaml
    /// </summary>
    public partial class KinectVizView : ViewBase
    {
        #region Members

        /// <summary>
        /// Input for Color Frames
        /// </summary>
        private ColorFrameSource colorFrameSource = null;

        /// <summary>
        /// Reader for color frames
        /// </summary>
        private ColorFrameReader colorFrameReader = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap colorBitmap = null;

        #endregion // Members

        #region Constructor
        public KinectVizView()
        {
            InitializeComponent();

            // clean up
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;

            // get the input of the color frames
            this.colorFrameSource = KinectModel.Instance.ColorFrameSource;

            // open the reader for the color frames
            this.colorFrameReader = this.colorFrameSource.OpenReader();
            
            // wire handler for frame arrival
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;

            // create the colorFrameDescription from the ColorFrameSource using Bgra format
            FrameDescription colorFrameDescription = this.colorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            // create the bitmap to display
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            // use the window object as the view model in this simple example
            this.DataContext = this;
        }
        #endregion // Constructor

        #region Events

        #region Dispatcher_ShutdownStarted
        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }
        }
        #endregion // Dispatcher_ShutdownStarted

        #region Reader_ColorFrameArrived
        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();
                    }
                }
            }
        }
        #endregion // Reader_ColorFrameArrived

        #endregion // Events

        #region Properties
        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }
        #endregion // Properties

    }
}
