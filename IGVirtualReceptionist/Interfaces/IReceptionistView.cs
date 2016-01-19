using IGVirtualReceptionist.Models;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IGVirtualReceptionist.Interfaces
{
    public interface IReceptionistView
    {
        void SpeechRecognized(SpeechRecognizedEventArgs e);
        void SpeechRecognitionRejected(SpeechRecognitionRejectedEventArgs e);
        Grammar GetGrammar();
        void GestureCaptured(GestureEventArgs args);
    }
}
