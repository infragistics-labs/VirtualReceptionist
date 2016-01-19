using IGVirtualReceptionist.Models;
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IGVirtualReceptionist.Gestures
{
	public delegate void GestureDetectedEventHandler(GestureEventArgs e);

	public class GestureDetector : IDisposable
	{

		public event GestureDetectedEventHandler GestureDetected;

		#region Members

		private KinectModel kinectModel;

		/// <summary> Path to the gesture database that was trained with VGB </summary>
		private readonly string gestureDatabase = @"GestureRepository\Wave3.gba";

		/// <summary> Name of the discrete gesture in the database that we want to track </summary>
		private readonly string waveGestureName = "Wave";

		/// <summary> Gesture frame source which should be tied to a body tracking ID </summary>
		private VisualGestureBuilderFrameSource vgbFrameSource = null;

		/// <summary> Gesture frame reader which will handle gesture events coming from the sensor </summary>
		private VisualGestureBuilderFrameReader vgbFrameReader = null;
		#endregion

		/// <summary>
		/// Initializes a new instance of the GestureDetector class along with the gesture frame source and reader
		/// </summary>
		/// <param name="kinectSensor">Active sensor to initialize the VisualGestureBuilderFrameSource object with</param>
		public GestureDetector(KinectModel model, KinectSensor kinectSensor)
		{
			this.kinectModel = model;

			if (kinectSensor == null)
			{
				throw new ArgumentNullException("kinectSensor");
			}

			// create the vgb source. The associated body tracking ID will be set when a valid body frame arrives from the sensor.
			this.vgbFrameSource = new VisualGestureBuilderFrameSource(kinectSensor, 0);
			this.vgbFrameSource.TrackingIdLost += this.Source_TrackingIdLost;

			// open the reader for the vgb frames
			this.vgbFrameReader = this.vgbFrameSource.OpenReader();
			if (this.vgbFrameReader != null)
			{
				this.vgbFrameReader.IsPaused = true;
				this.vgbFrameReader.FrameArrived += this.Reader_GestureFrameArrived;
			}

			// load the 'Seated' gesture from the gesture database
			using (VisualGestureBuilderDatabase database = new VisualGestureBuilderDatabase(this.gestureDatabase))
			{
				// we could load all available gestures in the database with a call to vgbFrameSource.AddGestures(database.AvailableGestures), 
				// but for this program, we only want to track one discrete gesture from the database, so we'll load it by name
				foreach (Gesture gesture in database.AvailableGestures)
				{
					//	TODO: MH - Add all gestures.
					if (gesture.Name.Equals(this.waveGestureName))
					{
						this.vgbFrameSource.AddGesture(gesture);
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the body tracking ID associated with the current detector
		/// The tracking ID can change whenever a body comes in/out of scope
		/// </summary>
		public ulong TrackingId
		{
			get
			{
				return this.vgbFrameSource.TrackingId;
			}

			set
			{
				if (this.vgbFrameSource.TrackingId != value)
				{
					this.vgbFrameSource.TrackingId = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether or not the detector is currently paused
		/// If the body tracking ID associated with the detector is not valid, then the detector should be paused
		/// </summary>
		public bool IsPaused
		{
			get
			{
				return this.vgbFrameReader.IsPaused;
			}

			set
			{
				if (this.vgbFrameReader.IsPaused != value)
				{
					this.vgbFrameReader.IsPaused = value;
				}
			}
		}

		/// <summary>
		/// Disposes all unmanaged resources for the class
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes the VisualGestureBuilderFrameSource and VisualGestureBuilderFrameReader objects
		/// </summary>
		/// <param name="disposing">True if Dispose was called directly, false if the GC handles the disposing</param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (this.vgbFrameReader != null)
				{
					this.vgbFrameReader.FrameArrived -= this.Reader_GestureFrameArrived;
					this.vgbFrameReader.Dispose();
					this.vgbFrameReader = null;
				}

				if (this.vgbFrameSource != null)
				{
					this.vgbFrameSource.TrackingIdLost -= this.Source_TrackingIdLost;
					this.vgbFrameSource.Dispose();
					this.vgbFrameSource = null;
				}
			}
		}

		/// <summary>
		/// Handles gesture detection results arriving from the sensor for the associated body tracking Id
		/// </summary>
		/// <param name="sender">object sending the event</param>
		/// <param name="e">event arguments</param>
		private void Reader_GestureFrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
		{
			VisualGestureBuilderFrameReference frameReference = e.FrameReference;
			using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame())
			{
				if (frame != null)
				{
					// get the discrete gesture results which arrived with the latest frame
					IReadOnlyDictionary<Gesture, DiscreteGestureResult> discreteResults = frame.DiscreteGestureResults;

					if (discreteResults != null)
					{
						// we only have one gesture in this source object, but you can get multiple gestures
						foreach (Gesture gesture in this.vgbFrameSource.Gestures)
						{
							if (gesture.Name.Equals(this.waveGestureName) && gesture.GestureType == GestureType.Discrete)
							{
								DiscreteGestureResult result = null;
								discreteResults.TryGetValue(gesture, out result);

								if (result != null)
								{
									GestureEventArgs args = new GestureEventArgs();
									args.Confidence = result.Confidence;
									args.Name = gesture.Name;

									//	Fire off event to KinectModel
									if (GestureDetected != null && args != null)
									{
										GestureDetected(args);
									}
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Handles the TrackingIdLost event for the VisualGestureBuilderSource object
		/// </summary>
		/// <param name="sender">object sending the event</param>
		/// <param name="e">event arguments</param>
		private void Source_TrackingIdLost(object sender, TrackingIdLostEventArgs e)
		{
			// update the GestureResultView object to show the 'Not Tracked' image in the UI
			//this.GestureResultView.UpdateGestureResult(false, false, 0.0f);
		}
	}
}
