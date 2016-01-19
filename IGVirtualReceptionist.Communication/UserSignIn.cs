using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Lync.Model;
using System.Windows.Forms;

namespace IGVirtualReceptionist.Communication
{
    class UserSignIn
    {
        public delegate void SetWindowCursorDelegate(Cursor newCursor);
        //Client state requires a change to the window cursor. 
        public event SetWindowCursorDelegate SetWindowCursor;

        public delegate void CloseAppConditionDelegate();
        //An error condition or client shut down requires parent window to close.
        public event CloseAppConditionDelegate CloseAppConditionHit;

        public delegate void UserIsSignedInDelegate();
        //User has signed in to Lync
        public event UserIsSignedInDelegate UserIsSignedIn;

        public delegate void ClientStateChangedDelegate(string newState);
        //The state of the Lync client has changed.
        public event ClientStateChangedDelegate ClientStateChanged;

        /// <summary>
        /// Flag that indicates that this instance of the ShareResources
        /// process initialized Lync. Other instances of ShareResources must not
        /// attempt to shut down Lync
        /// </summary>
        private Boolean _thisProcessInitializedLync = false;

        /// <summary>
        /// Indicates the user is starting a Side-by-side instance of Lync
        /// </summary>
        private Boolean _inSideBySideMode = false;

        /// <summary>
        /// Lync client platform. The entry point to the API
        /// </summary>
        Microsoft.Lync.Model.LyncClient _LyncClient;

        //ShareResources.ShareResources_Form _shareResources;
        string _UserUri;

        public Microsoft.Lync.Model.LyncClient Client
        {
            get
            {
                return _LyncClient;
            }
        }
        public Boolean ThisProcessInitializedLync
        {
            get
            {
                return _thisProcessInitializedLync;
            }
        }
        
        public UserSignIn(LyncClient lyncClient)
        {
            _LyncClient = lyncClient;
        }

        /// <summary>
        /// Gets the Lync client, initializes if in UI suppression, and 
        /// starts the user sign in process
        /// </summary>
        /// <param name="sideBySide">boolean. Specifies endpoint mode</param> 
        internal void StartUpLync(Boolean sideBySide)
        {
            //Calling GetClient a second time in a running process will
            //return the previously cached client. For example, calling GetClient(boolean sideBySideFlag)
            // the first time in a process returns a new endpoint.  Calling the method a second
            //time returns the original endpoint. If you call GetClient(false) to get a client 
            //endpoint and then GetClient(true), the original client enpoint is returned even though
            // a true value argument is passed with the second call.

            try
            {
                if (_LyncClient == null)
                {
                    //If sideBySide == false, a standard endpoint is created
                    //Otherwise, a side-by-side endpoint is created
                    _LyncClient = LyncClient.GetClient(sideBySide);
                }

                //if (_LyncClient.Self.Contact == null)
                //{
                //    foreach (var process in System.Diagnostics.Process.GetProcessesByName(Constants.PROCESSNAME))
                //    {
                //        process.Kill();
                //    }

                //    var unused = _LyncClient.State;
                //    _LyncClient = LyncClient.GetClient();
                //}
                if (_LyncClient.State == ClientState.Invalid)
                    _LyncClient = LyncClient.GetClient(false);

                _inSideBySideMode = sideBySide;

                //Display the current state of the Lync client.
                if (ClientStateChanged != null)
                {
                    ClientStateChanged(_LyncClient.State.ToString());
                }

                //Register for the three Lync client events needed so that application is notified when:
                // * Lync client signs in or out
                _LyncClient.StateChanged += _LyncClient_StateChanged;
                _LyncClient.SignInDelayed += _LyncClient_SignInDelayed;
                _LyncClient.CredentialRequested += _LyncClient_CredentialRequested;



                //Client state of uninitialized means that Lync is configured for UI suppression mode and
                //must be initialized before a user can sign in to Lync
                if (_LyncClient.State == ClientState.Uninitialized)
                {
                    _LyncClient.BeginInitialize(
                        (ar) =>
                        {
                            _LyncClient.EndInitialize(ar);
                            _thisProcessInitializedLync = true;
                        },
                        null);
                }

                //If the Lync client is signed out, sign into the Lync client
                
                if (_LyncClient.State == ClientState.SignedOut)
                {
                    SignUserIn();
                }

                // don't wait more than 5 seconds.
                int waitCounter = 0;
                while (_LyncClient.State == ClientState.SigningIn && waitCounter < 50)
                {
                    //if (MessageBox.Show(
                    //    "Lync is signing in. Do you want to continue waiting?",
                    //    "Sign in delay",
                    //    MessageBoxButtons.YesNo,
                    //    MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.No)
                    //{
                    //    if (CloseAppConditionHit != null)
                    //    {
                    //        CloseAppConditionHit();
                    //    }

                    //}
                    System.Threading.Thread.Sleep(100);
                    waitCounter++;
                }
            }
            catch (NotInitializedException)
            {
                MessageBox.Show(
                    "Client is not initialized.  Closing form",
                    "Lync Client Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Hand);
                if (CloseAppConditionHit != null)
                {
                    CloseAppConditionHit();
                }

            }
            catch (ClientNotFoundException)
            {
                MessageBox.Show(
                    "Client is not running.  Closing form",
                    "Lync Client Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Hand);
                if (CloseAppConditionHit != null)
                {
                    CloseAppConditionHit();
                }

            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    "General exception: " +
                    exc.Message, "Lync Client Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Hand);
                if (CloseAppConditionHit != null)
                {
                    CloseAppConditionHit();
                }

            }
        }

        /// <summary>
        /// Raised when user's credentials are rejected by Lync or a service that
        /// Lync depends on requests credentials
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _LyncClient_CredentialRequested(object sender, CredentialRequestedEventArgs e)
        {
            //If the request for credentials comes from Lync server then sign out, get new creentials
            //and sign in.
            if (e.Type == CredentialRequestedType.LyncAutodiscover)
            {
                try
                {
                    _LyncClient.BeginSignOut((ar) =>
                    {
                        _LyncClient.EndSignOut(ar);
                        //Ask user for credentials and attempt to sign in again
                        SignUserIn();
                    }, null);
                }
                catch (Exception ex)
                {
                    if (SetWindowCursor != null)
                    {
                        SetWindowCursor(Cursors.Arrow);
                    }
                    MessageBox.Show(
                        "Exception on attempt to sign in, abandoning sign in: " +
                        ex.Message,
                        "Lync Client sign in delay",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                e.Submit(Constants.ID, Constants.PASSWORD, false);
            }
        }

        void _LyncClient_SignInDelayed(object sender, SignInDelayedEventArgs e)
        {
            if (MessageBox.Show(
                "Delay started at " +
                e.EstimatedStartDelay.ToString() +
                " Status code:" +
                e.StatusCode.ToString(),
                "Lync Client sign in delay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.No)
            {
                if (CloseAppConditionHit != null)
                {
                    CloseAppConditionHit();
                }
            }
            else
            {
                try
                {
                    _LyncClient.BeginSignOut((ar) => { _LyncClient.EndSignOut(ar); }, null);
                }
                catch (LyncClientException lce)
                {
                    MessageBox.Show("Exception on sign out in SignInDelayed event: " + lce.Message);
                }
            }

        }
        /// <summary>
        /// Handles the event raised when a user signs in to or out of the Lync client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _LyncClient_StateChanged(object sender, ClientStateChangedEventArgs e)
        {

            System.Diagnostics.Debug.WriteLine("StateChanged: ({0}) ({1})", e.NewState, e.OldState);
            switch (e.NewState)
            {
                case ClientState.SignedOut:
                    if (e.OldState == ClientState.Initializing)
                    {
                        SignUserIn();
                    }
                    if (e.OldState == ClientState.SigningOut)
                    {
                        _LyncClient.BeginShutdown((ar) =>
                        {
                            _LyncClient.EndShutdown(ar);
                        }, null);
                    }
                    break;
                case ClientState.Uninitialized:
                    if (e.OldState == ClientState.ShuttingDown)
                    {
                        _LyncClient.StateChanged -= _LyncClient_StateChanged;
                        try
                        {
                            if (CloseAppConditionHit != null)
                            {
                                CloseAppConditionHit();
                            }
                        }
                        catch (InvalidOperationException oe)
                        {
                            System.Diagnostics.Debug.WriteLine("Invalid operation exception on close: " + oe.Message);
                        }
                    }
                    break;
                case ClientState.SignedIn:
                    if (UserIsSignedIn != null)
                    {
                        UserIsSignedIn();
                    }
                    break;
            }
            if (ClientStateChanged != null)
            {
                ClientStateChanged(e.NewState.ToString());
            }


        }

        /// <summary>
        /// Signs a user in to Lync as one of two possible users. User that is
        /// signed in depends on whether side-by-side client is chosen.
        /// </summary>
        public void SignUserIn()
        {
            //Set the display cursor to indicate that user must wait for
            //sign in to complete
            if (SetWindowCursor != null)
            {
                SetWindowCursor(Cursors.WaitCursor);
            }

            //Set the sign in credentials of the user to the
            //appropriate credentials for the endpoint mode
            string userUri = Constants.ID;
            string userName = Constants.ID;
            string userPassword = Constants.PASSWORD;

            _UserUri = userUri;

            _LyncClient.BeginSignIn(
                userUri,
                userName,
                userPassword,
                (ar) =>
                {
                    try
                    {
                        _LyncClient.EndSignIn(ar);
                        if (UserIsSignedIn != null)
                        {
                            UserIsSignedIn();
                        }

                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("exception on endsignin: " + exc.Message);
                    }
                },
                null);

        }

    }
}
