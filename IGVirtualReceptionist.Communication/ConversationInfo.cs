using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
using Microsoft.Lync.Model.Conversation.AudioVideo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IGVirtualReceptionist.Communication
{
    internal class ConversationInfo : IDisposable
    {
        #region Members

        private LyncClient client;
        private Conversation conversation;
        private IntPtr parentHandle;
        private Rectangle bounds;
        private AsyncCallback videoCallStartedCallback;
        private AsyncCallback conversationEndedCallback;

        //self participant's channels
        private VideoChannel videoChannel;

        #endregion //Members

        #region Constructor

        internal ConversationInfo(Conversation conversation, LyncClient client, IntPtr parentHandle, Rectangle bounds, AsyncCallback conversationEndedCallback, AsyncCallback videoCallStartedCallback)
        {
            this.conversation = conversation;
            this.client = client;
            this.parentHandle = parentHandle;
            this.bounds = bounds;
            this.conversationEndedCallback = conversationEndedCallback;
            this.videoCallStartedCallback = videoCallStartedCallback;

            this.videoChannel = ((AVModality)conversation.Modalities[ModalityTypes.AudioVideo]).VideoChannel;

            //subscribes to the video channel state changes so that the video feed can be presented
            videoChannel.StateChanged += videoChannel_StateChanged;
        }

        #endregion //Constructor

        #region Properties

        #region Contact

        internal Contact Contact
        {
            get
            {
                foreach (Participant participant in conversation.Participants)
                    if (participant.IsSelf == false)
                        return participant.Contact;
                return null;
            }
        }

        #endregion //Contact

        #region Conversation

        internal Conversation Conversation
        {
            get
            {
                if (this.conversation == null)
                {

                }
                return this.conversation;
            }
        }

        #endregion //Conversation

        #region ConversationEnded

        internal AsyncCallback ConversationEnded
        {
            get
            {
                return this.conversationEndedCallback;
            }
        }

        #endregion //ConversationEnded

        #region VideoCallStarted

        internal AsyncCallback VideoCallStarted
        {
            get
            {
                return this. videoCallStartedCallback;
            }
        }

        #endregion //VideoCallStarted

        #endregion //Properties

        #region Methods

        #region Dispose

        public void Dispose()
        {

            videoChannel.StateChanged -= videoChannel_StateChanged;

            this.client = null;
            this.conversation = null;
            this.videoChannel = null;
        }

        #endregion //Dispose

        #region ShowVideo

        /// <summary>
        /// Shows the specified video window in the specified panel.
        /// </summary>
        private static void ShowVideo(IntPtr parentHandle, Rectangle bounds, VideoWindow videoWindow)
        {

            //Win32 constants:                  WS_CHILD | WS_CLIPCHILDREN | WS_CLIPSIBLINGS;
            const long lEnableWindowStyles = 0x40000000L | 0x02000000L | 0x04000000L;
            //Win32 constants:                   WS_POPUP| WS_CAPTION | WS_SIZEBOX
            const long lDisableWindowStyles = 0x80000000 | 0x00C00000 | 0x00040000L;
            const int OATRUE = -1;

            try
            {
                //sets the properties required for the native video window to draw itself
                var handle = (IntPtr.Size == 4) ? parentHandle.ToInt32() : parentHandle.ToInt64();
                videoWindow.Owner = handle;
                videoWindow.SetWindowPosition(bounds.X, bounds.Y, bounds.Width, bounds.Height);

                //gets the current window style to modify it
                long currentStyle = videoWindow.WindowStyle;

                //disables borders, sizebox, close button
                currentStyle = currentStyle & ~lDisableWindowStyles;

                //enables styles for a child window
                currentStyle = currentStyle | lEnableWindowStyles;

                //updates the current window style
                videoWindow.WindowStyle = (int)currentStyle;

                //updates the visibility
                videoWindow.Visible = OATRUE;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        #endregion

        #region StartVideo

        internal void StartVideo()
        {

            //starts a video call or the video stream in a audio call
            AsyncCallback callback = new AsyncOperationHandler(videoChannel.EndStart).Callback;
            try
            {
                videoChannel.BeginStart(callback, null);
            }
            catch (LyncClientException lyncClientException)
            {
                Console.WriteLine(lyncClientException);
            }
            catch (SystemException systemException)
            {
                if (Utilities.IsLyncException(systemException))
                {
                    // Log the exception thrown by the Lync Model API.
                    Console.WriteLine("Error: " + systemException);
                }
                else
                {
                    // Rethrow the SystemException which did not come from the Lync Model API.
                    throw;
                }
            }
        }

        #endregion //StartVideo

        #region StopVideo

        internal void StopVideo()
        {
                        //removes video from the conversation
            AsyncCallback callback = new AsyncOperationHandler(videoChannel.EndStop).Callback;
            try
            {
                videoChannel.BeginStop(callback, null);

            }
            catch (LyncClientException lyncClientException)
            {
                Console.WriteLine(lyncClientException);
            }
            catch (SystemException systemException)
            {
                if (Utilities.IsLyncException(systemException))
                {
                    // Log the exception thrown by the Lync Model API.
                    Console.WriteLine("Error: " + systemException);
                }
                else
                {
                    // Rethrow the SystemException which did not come from the Lync Model API.
                    throw;
                }
            }
        }

        #endregion //StopVideo

        #endregion //Methods

        #region Event Handlers

        #region videoChannel_StateChanged

        /// <summary>
        /// Called when the video state changes.
        /// 
        /// Will show Incoming/Outgoing video based on the channel state.
        /// </summary>
        void videoChannel_StateChanged(object sender, ChannelStateChangedEventArgs e)
        {
            //posts the execution into the UI thread
            CommunicationManager.UiThreadControl.BeginInvoke(new MethodInvoker(delegate()
            {

                //*****************************************************************************************
                //                              Video Content
                //
                // The video content is only available when the Lync client is running in UISuppressionMode.
                //
                // The video content is not directly accessible as a stream. It's rather available through
                // a video window that can de drawn in any panel or window.
                // 
                // The incoming video is accessible from videoChannel.RenderVideoWindow
                // The window will be available when the video channel state is either Receive or SendReceive.
                //
                //*****************************************************************************************

                //TODO: Temprary to see my own window
                //if the outgoing video is now active, show the video (which is only available in UI Suppression Mode)
                if ((e.NewState == ChannelState.Send 
                    || e.NewState == ChannelState.SendReceive) && videoChannel.CaptureVideoWindow != null)
                {

                    if (this.VideoCallStarted != null)
                        this.VideoCallStarted(null);

                    //presents the video in the panel
                    ShowVideo(parentHandle, bounds, videoChannel.CaptureVideoWindow);
                }

                // switch to see incoming
                ////if the incoming video is now active, show the video (which is only available in UI Suppression Mode)
                //if ((e.NewState == ChannelState.Receive 
                //    || e.NewState == ChannelState.SendReceive) && videoChannel.RenderVideoWindow != null)
                //{
                //    //presents the video in the panel
                //    ShowVideo(parentHandle, bounds, videoChannel.CaptureVideoWindow);
                //}

            }));
        }

        #endregion //videoChannel_StateChanged

        #endregion //Event Handlers


    }
}
