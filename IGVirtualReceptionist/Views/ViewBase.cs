using IGVirtualReceptionist.Interfaces;
using System;
using System.Windows.Controls;
using IGVirtualReceptionist.Models;
using Microsoft.Speech.Recognition;

namespace IGVirtualReceptionist.Views
{
    public class ViewBase : UserControl, IReceptionistView
    {
        public ViewBase()
        {
            this.IsVisibleChanged += (p1, p2) =>
            {
                if (this.IsVisible)
                    KinectModel.Instance.RegisterActiveView(this);
                else
                    KinectModel.Instance.UnregisterActiveView(this);
            };
        }
        
        public virtual void GestureCaptured(GestureEventArgs args)
        {
        }

        public virtual Grammar GetGrammar()
        {
            return null;
        }

        public virtual void SpeechRecognitionRejected(SpeechRecognitionRejectedEventArgs e)
        {
        }

        public virtual void SpeechRecognized(SpeechRecognizedEventArgs e)
        {
        }
    }
}
