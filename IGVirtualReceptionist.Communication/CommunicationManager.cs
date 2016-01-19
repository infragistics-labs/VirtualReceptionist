using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
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
    public class CommunicationManager
    {
        #region Members

        private static CommunicationManager instance;

        internal static Control UiThreadControl = new Control();
        private LyncClient _LyncClient;
        private AutoResetEvent conversationWindowLock = new AutoResetEvent(false);

        // Saves a list of the conversation windows, so they can be closed
        // when a conversation is removed.        
        private Dictionary<Conversation, ConversationInfo> ConversationInfos = new Dictionary<Conversation, ConversationInfo>();
        private IntPtr lastHandle;
        private Rectangle lastBounds;
        private AsyncCallback lastConversationEndedCallback;
        private AsyncCallback lastVideoCallStartedCallback;

        #endregion Members

        #region Constructor

        private CommunicationManager()
        {
            var unused = UiThreadControl.Handle;

            while (this._LyncClient == null)
            {
                try
                {
                    //obtains the lync client instance
                    this._LyncClient = LyncClient.GetClient();
                }
                //if the Lync process is not running and UISuppressionMode=false, this exception will be thrown
                catch (ClientNotFoundException)
                {
                    //explain to the user what happened
                    if (MessageBox.Show("Microsoft Lync does not appear to be running. Please start Skype For Business.", "Skype for Business not found", MessageBoxButtons.RetryCancel) == DialogResult.Retry)
                    {
                        continue;
                    }
                    throw;
                }
                catch (NotStartedByUserException)
                {
                    //explain to the user what happened
                    if (MessageBox.Show("Microsoft Lync does not appear to be running. Please start Skype For Business.", "Skype for Business not found", MessageBoxButtons.RetryCancel) == DialogResult.Retry)
                    {
                        continue;
                    }
                    throw;
                }

                if (this._LyncClient == null)
                {
                    Application.Exit();
                }

            }

            this.InitializeClient();
        }

        #endregion //Constructor

        #region Properties

        #region Instance

        internal static CommunicationManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new CommunicationManager();
                return instance;
            }
        }

        #endregion //Instance

        #region Client

        internal Client Client
        {
            get
            {
                if (this._LyncClient == null)
                    this._LyncClient = LyncClient.GetClient();

                return this._LyncClient;
            }
        }

        #endregion //Client

        #region ContactManager

        private ContactManager ContactManager
        {
            get
            {
                return this.Client.ContactManager;
            }
        }

        #endregion //ContactManager

        #endregion //Properties

        #region Methods

        #region ClientInitialized

        /// <summary>
        /// Called when the client in done initializing.
        /// </summary>
        /// <param name="result"></param>
        private void ClientInitialized(IAsyncResult result)
        {
            //registers for conversation related events
            //these events will occur when new conversations are created (incoming/outgoing) and removed
            _LyncClient.ConversationManager.ConversationAdded += ConversationManager_ConversationAdded;
            _LyncClient.ConversationManager.ConversationRemoved += ConversationManager_ConversationRemoved;

        }

        #endregion //ClientInitialized

        #region CreateConversation

        private Conversation CreateConversation(string id)
        {
            Contact contact = this.GetContact(id);

            Conversation conversation = this.Client.ConversationManager.AddConversation();
            if (contact != null && conversation != null)
            {
                try
                {
                    conversation.AddParticipant(contact);
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

            return conversation;
        }

        #endregion //CreateConversation

        #region GetContact

        private Contact GetContact(string userId)
        {
            Contact contact = null;

            //Finds the contact using the provided URI (synchronously)
            try
            {
                contact = this.ContactManager.GetContactByUri(userId);
            }
            catch (LyncClientException lyncClientException)
            {
                Console.WriteLine("Contact not found.  Did you use the sip: or tel: prefix? " + lyncClientException);
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
            return contact;
        }

        #endregion //GetContact

        #region HangUpVideoCall

        public static void HangUpVideoCall(string userId)
        {
            Instance.HangUpVideoCallInstance(userId);
        }

        #endregion HangUpVideoCall

        #region HangUpVideoCallInstance

        private void HangUpVideoCallInstance(string userId)
        {
            Contact contact = this.ContactManager.GetContactByUri(userId);
            if (contact == null)
                return;

            ConversationInfo info = this.ConversationInfos.Values.Where(convInfo => convInfo.Contact == contact).FirstOrDefault();
            if (info != null)
            {
                var conversation = info.Conversation;
                this.ConversationInfos.Remove(conversation);
                info.StopVideo();

                if (info.ConversationEnded != null)
                    info.ConversationEnded(null);

                info.Dispose();

                //if the conversation is active, will end it
                if (conversation.State != ConversationState.Terminated)
                {
                    //ends the conversation which will disconnect all modalities
                    try
                    {
                        conversation.End();
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
            }
        }

        #endregion HangUpVideoCallInstance

        #region InitializeClient

        private void InitializeClient()
        {

            UserSignIn signin = new UserSignIn(this._LyncClient);
            signin.StartUpLync(false);

            //***********************************************************************************
            // This application works with UISuppressionMode = true or false
            //
            // UISuppressionMode hides the Lync user interface.
            //
            // Registry key for enabling UISuppressionMode:
            //
            // 32bit OS:
            // [HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Office\15.0\Lync]
            // "UISuppressionMode"=dword:00000001
            //
            // 64bit OS:
            // [HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Communicator]
            // "UISuppressionMode"=dword:00000001
            //
            // When running with UISuppressionMode = 1 and this application is the only one
            // using the client, it's necessary to Initialize the client. The following check
            // verifies if the client has already been initialized. If it hasn't, the code will
            // call BeginInitialize() proving a callback method, on which this application's
            // main UI will be presented (either Sign-In or contact input, if already signed in).
            //***********************************************************************************

            //if this client is in UISuppressionMode...
            if (_LyncClient.InSuppressedMode && _LyncClient.State == ClientState.Uninitialized)
            {
                //...need to initialize it
                try
                {
                    _LyncClient.BeginInitialize(this.ClientInitialized, null);
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
            else //not in UI Suppression, so the client was already initialized
            {
                //registers for conversation related events
                //these events will occur when new conversations are created (incoming/outgoing) and removed
                _LyncClient.ConversationManager.ConversationAdded += ConversationManager_ConversationAdded;
                _LyncClient.ConversationManager.ConversationRemoved += ConversationManager_ConversationRemoved;

                //show sign-in or contact selection
                //ShowMainContent();
            }
        }

        #endregion //InitializeClient        

        #region InitiateVideoCall

        public static void InitiateVideoCall(IntPtr parentContainerHandle, int x, int y, int width, int height, string userId, AsyncCallback conversationEndedCallback, AsyncCallback videoCallStartedCallback)
        {
            Rectangle bounds = new Rectangle(x, y, width, height);
            Instance.InitiateVideoCallInstance(parentContainerHandle, bounds, userId, conversationEndedCallback, videoCallStartedCallback);
        }

        public static void InitiateVideoCall(IntPtr parentContainerHandle, Rectangle bounds, string userId, AsyncCallback conversationEndedCallback, AsyncCallback videoCallStartedCallback)
        {
            Instance.InitiateVideoCallInstance(parentContainerHandle, bounds, userId, conversationEndedCallback, videoCallStartedCallback);
        }

        #endregion //InitiateVideoCall

        #region InitiateVideoCallInstance

        private void InitiateVideoCallInstance(IntPtr parentContainerHandle, Rectangle bounds, string userId, AsyncCallback conversationEndedCallback, AsyncCallback videoCallStartedCallback)
        {
            this.lastHandle = parentContainerHandle;
            this.lastBounds = bounds;
            this.lastConversationEndedCallback = conversationEndedCallback;
            this.lastVideoCallStartedCallback = videoCallStartedCallback;

            Conversation conversation = this.CreateConversation(userId);
        }

        #endregion //InitiateVideoCallInstance

        #endregion //Methods

        #region Event Handles

        #region ConversationManager event handling

        //*****************************************************************************************
        //                              ConversationManager Event Handling
        // 
        // ConversationAdded occurs when:
        // 1) A new conversation was created by this application
        // 2) A new conversation was created by another third party application or Lync itself
        // 2) An invite was received at this endpoint (InstantMessaging / AudioVideo)
        //
        // ConversationRemoved occurs when:
        // 1) A conversation is terminated
        //
        //*****************************************************************************************

        /// <summary>
        /// Called when a new conversation is added (incoming or outgoing).
        /// 
        /// Will create a window for this new conversation and show it.
        /// </summary>
        void ConversationManager_ConversationAdded(object sender, ConversationManagerEventArgs e)
        {

            //*****************************************************************************************
            //                              Registering for events
            //
            // It is very important that registering for an object's events happens within the handler
            // of that object's added event. In another words, the application should register for the
            // conversation events within the ConversationAdded event handler.
            //
            // This is required to avoid timing issues which would cause the application to miss events.
            // While this handler method is executing, the Lync client is unable to process events for  
            // this application (synce its thread is running this method), so no events will be lost.
            //
            // By registering for events here, we guarantee that all conversation related events will be 
            // caught the first time they occur.
            //
            // We want to show the availability of the buttons in the conversation window based
            // on the ActionAvailability events. The solution below uses a lock to allow the window
            // to load while holding the event queue. This prevents events from being raised even 
            // before the user interface controls get a change to load.
            //
            //*****************************************************************************************


            //posts the execution into the UI thread
            UiThreadControl.BeginInvoke(new MethodInvoker(delegate()
            {
                //shows the new window
                //window.Show(this);

                //creates a new window (which will register for Conversation and child object events)
                ConversationInfo info = new ConversationInfo(e.Conversation, _LyncClient, this.lastHandle, this.lastBounds, this.lastConversationEndedCallback, this.lastVideoCallStartedCallback);
                this.ConversationInfos.Add(e.Conversation, info);

                this.lastConversationEndedCallback = null;
                this.lastVideoCallStartedCallback = null;
                //conversationWindowLock.Set();

                info.StartVideo();
            }));

            //waits until the window is loaded to release the SDK thread
            //conversationWindowLock.WaitOne();
        }


        /// <summary>
        /// Called when a conversation is removed.
        /// 
        /// Will dispose the window associated with the removed conversation.
        /// </summary>
        void ConversationManager_ConversationRemoved(object sender, ConversationManagerEventArgs e)
        {
            //posts the execution into the UI thread
            UiThreadControl.BeginInvoke(new MethodInvoker(delegate()
            {
                //checks if a conversation window was created, and dispose it
                if (this.ConversationInfos.ContainsKey(e.Conversation))
                {
                    //gets the existing conversation window
                    ConversationInfo info = ConversationInfos[e.Conversation];

                    //remove the conversation from the dictionary
                    this.ConversationInfos.Remove(e.Conversation);
                    
                    if (info.ConversationEnded != null)
                        info.ConversationEnded(null);

                    //cleanup
                    info.Dispose();
                }

            }));
        }

        #endregion

        #endregion //Event Handles

    }
}
