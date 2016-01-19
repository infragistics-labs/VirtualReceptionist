using IGVirtualReceptionist.Gestures;
using IGVirtualReceptionist.Helpers;
using IGVirtualReceptionist.Interfaces;
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace IGVirtualReceptionist.Models
{
    public class KinectModel
    {
        private static readonly KinectModel instance = new KinectModel();
        public static KinectModel Instance
        {
            get
            {
                return instance;
            }
        }

        private KinectSensor kinectSensor = null;
        private KinectAudioStream convertStream = null;
        private SpeechRecognitionEngine speechEngine = null;
        private RecognizerInfo ri = null;

		/// <summary> Array for the bodies (Kinect will track up to 6 people simultaneously) </summary>
		private Body[] bodies = null;

        private List<Body> trackedBodies = new List<Body>();
        private bool IsInTrackingBodyState = false;

        private DispatcherTimer timerNoBodies = new DispatcherTimer();
        private DispatcherTimer timerFoundBodies = new DispatcherTimer();
        public event EventHandler<BodyTrackingEventArgs> StoppedTrackingBodies;
        public event EventHandler<BodyTrackingEventArgs> StartedTrackingNewBodies;

		/// <summary> Reader for body frames </summary>
		private BodyFrameReader bodyFrameReader = null;

		/// <summary> List of gesture detectors, there will be one detector created for each potential body (max of 6) </summary>
		private List<GestureDetector> gestureDetectorList = null;

        private List<IReceptionistView> ActiveViews;

        private ColorFrameSource colorFrameSource = null;

        private KinectModel()
        {
            kinectSensor = KinectSensor.GetDefault();
            timerFoundBodies.Interval = TimeSpan.FromSeconds(1.5);
            timerNoBodies.Interval = TimeSpan.FromSeconds(10);
            timerFoundBodies.Tick += TimerFoundBodies_Tick;
            timerNoBodies.Tick += TimerNoBodies_Tick;

            if (kinectSensor != null)
            {
                this.kinectSensor.Open();

                IReadOnlyList<AudioBeam> audioBeamList = this.kinectSensor.AudioSource.AudioBeams;
                Stream audioStream = audioBeamList[0].OpenInputStream();

                this.convertStream = new KinectAudioStream(audioStream);
            }
            else
            {
                return;
            }

            ri = TryGetKinectRecognizer();
            //	Setup Gestures
            InitGestures();

            ActiveViews = new List<IReceptionistView>();
        }

        #region Speech
        private void InitializeSpeech(Grammar baseGrammar)
        {
            if (ri != null)
            {
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                LoadGrammar(baseGrammar);

                this.speechEngine.SpeechRecognized += SpeechEngine_SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected += SpeechEngine_SpeechRecognitionRejected;

                this.convertStream.SpeechActive = true;

                this.speechEngine.SetInputToAudioStream(this.convertStream,
                    new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));

                this.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        private void SpeechEngine_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            NotifySpeechRecognitionRejected(e);
        }

        private void SpeechEngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence > 0.8f)
            {
                NotifySpeechRecognized(e);
            }
        }

        private void NotifySpeechRecognitionRejected(SpeechRecognitionRejectedEventArgs e)
        {
            foreach (IReceptionistView view in ActiveViews)
            {
                view.SpeechRecognitionRejected(e);
            }
        }

        private void NotifySpeechRecognized(SpeechRecognizedEventArgs e)
        {
            foreach (IReceptionistView view in ActiveViews)
            {
                view.SpeechRecognized(e);
            }
        }

        private static RecognizerInfo TryGetKinectRecognizer()
        {
            IEnumerable<RecognizerInfo> recognizers;

            try
            {
                recognizers = SpeechRecognitionEngine.InstalledRecognizers();
            }
            catch (COMException)
            {
                return null;
            }

            foreach (RecognizerInfo recognizer in recognizers)
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        private void LoadGrammar(Grammar g)
        {
            if (g == null)
                return;

			if (this.speechEngine != null)
			{
				this.speechEngine.LoadGrammar(g);
			}
            else if (this.kinectSensor != null)
            {
                InitializeSpeech(g);
            }
        }

        private void UnloadGrammar(Grammar g)
        {
            if (g == null)
                return;

			if (this.speechEngine != null)
			{
				this.speechEngine.UnloadGrammar(g);
			}
        }
		#endregion

        #region ColorFrameSource

        internal ColorFrameSource ColorFrameSource
        {
            get
            {
                if (this.colorFrameSource == null)
                {
                    this.colorFrameSource = this.kinectSensor.ColorFrameSource;
                }
                return this.colorFrameSource;
            }
        }

        #endregion // ColorFrameSource

		#region Gestures

		private void InitGestures()
		{
            if (Utilities.IsDesignMode)
                return;

			// open the reader for the body frames
			//	This may need to be opened on a more higher level
			this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

			// set the BodyFramedArrived event notifier
			this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;

			this.gestureDetectorList = new List<GestureDetector>();

			//	Setup Gestures for each potential skeleton
			int maxBodies = this.kinectSensor.BodyFrameSource.BodyCount;
			for (int i = 0; i < maxBodies; ++i)
			{
				GestureDetector detector = new IGVirtualReceptionist.Gestures.GestureDetector(this, this.kinectSensor);

				detector.GestureDetected += detector_GestureDetected;
				this.gestureDetectorList.Add(detector);
			}
		}

		private void detector_GestureDetected(GestureEventArgs e)
		{
			NotifyGestureCaptured(e);
		}

		private void NotifyGestureCaptured(GestureEventArgs args)
		{
            // put in some threshold
            if (args.Confidence < 0.1)
                return;

			foreach (IReceptionistView view in ActiveViews)
			{
				view.GestureCaptured(args);
		}
		}

		private void UnloadGestures()
		{
			if (this.gestureDetectorList != null)
			{
				// The GestureDetector contains disposable members (VisualGestureBuilderFrameSource and VisualGestureBuilderFrameReader)
				foreach (GestureDetector detector in this.gestureDetectorList)
				{
					detector.Dispose();
				}

				this.gestureDetectorList.Clear();
				this.gestureDetectorList = null;
			}
		}

		#region BodyFrameArrived Event Handler
		private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
		{
			bool dataReceived = false;

			using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
			{
				if (bodyFrame != null)
				{
					if (this.bodies == null)
					{
						// creates an array of 6 bodies, which is the max number of bodies that Kinect can track simultaneously
						this.bodies = new Body[bodyFrame.BodyCount];
					}

					// The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
					// As long as those body objects are not disposed and not set to null in the array,
					// those body objects will be re-used.
					bodyFrame.GetAndRefreshBodyData(this.bodies);
					dataReceived = true;
				}
			}

			if (dataReceived)
			{
				// we may have lost/acquired bodies, so update the corresponding gesture detectors
				if (this.bodies != null)
				{
                    trackedBodies = new List<Body>();

					// loop through all bodies to see if any of the gesture detectors need to be updated
					int maxBodies = this.kinectSensor.BodyFrameSource.BodyCount;
					for (int i = 0; i < maxBodies; ++i)
					{
						Body body = this.bodies[i];
						ulong trackingId = body.TrackingId;
                        if (body.IsTracked)
                            trackedBodies.Add(body);

						// if the current body TrackingId changed, update the corresponding gesture detector with the new value
						if (trackingId != this.gestureDetectorList[i].TrackingId)
						{
							this.gestureDetectorList[i].TrackingId = trackingId;

							// if the current body is tracked, unpause its detector to get VisualGestureBuilderFrameArrived events
							// if the current body is not tracked, pause its detector so we don't waste resources trying to get invalid gesture results
							this.gestureDetectorList[i].IsPaused = trackingId == 0;
						}
					}
				}
			}

            if (trackedBodies.Count == 0)
            {
                if (timerFoundBodies.IsEnabled)
                    timerFoundBodies.Stop();

                if (!timerNoBodies.IsEnabled && IsInTrackingBodyState)
                    timerNoBodies.Start();
            }
            else
            {
                if (timerNoBodies.IsEnabled)
                    timerNoBodies.Stop();

                if (!timerFoundBodies.IsEnabled && !IsInTrackingBodyState)
                    timerFoundBodies.Start();
            }
		}
        #endregion

        private void TimerNoBodies_Tick(object sender, EventArgs e)
        {
            IsInTrackingBodyState = false;
            timerNoBodies.Stop();
           
            EventHandler<BodyTrackingEventArgs> handler = StoppedTrackingBodies;
            if (handler != null)
                handler(this, new BodyTrackingEventArgs() { TrackedBodies = trackedBodies } );
        }

        private void TimerFoundBodies_Tick(object sender, EventArgs e)
        {
            IsInTrackingBodyState = true;
            timerFoundBodies.Stop();

            EventHandler<BodyTrackingEventArgs> handler = StartedTrackingNewBodies;
            if (handler != null)
                handler(this, new BodyTrackingEventArgs() { TrackedBodies = trackedBodies });
        }

        #endregion

        public void RegisterActiveView(IReceptionistView view)
        {
            this.ActiveViews.Add(view);
            LoadGrammar(view.GetGrammar());
        }

        public void UnregisterActiveView(IReceptionistView view)
        {
            UnloadGrammar(view.GetGrammar());
            this.ActiveViews.Remove(view);
        }
    }

    public class BodyTrackingEventArgs
    {
        public List<Body> TrackedBodies { get; set; }
    }
	public class GestureEventArgs
	{
		public String Name { get; set; }
		public float Confidence { get; set; }
		public ulong TrackingId { get; set; }
	}
}
