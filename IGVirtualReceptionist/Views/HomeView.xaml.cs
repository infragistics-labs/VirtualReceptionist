using IGVirtualReceptionist.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Speech.Recognition;
using System.IO;
using IGVirtualReceptionist.Models;

namespace IGVirtualReceptionist.Views
{
    /// <summary>
    /// Interaction logic for ucVRHome.xaml
    /// </summary>
    public partial class HomeView : ViewBase
    {
        #region Members

        private Grammar homeGrammar;
        //private DirectoryModel Directory;

        #endregion //Members

        #region Constructor

        public HomeView()
        {
            InitializeComponent();

            //Directory = new DirectoryModel();

            using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(Properties.Resources.HomeGrammar)))
            {
                homeGrammar = new Grammar(memoryStream);
            }

            this.LoadExpectedVisitors();
        }

        #endregion //Constructor

        #region Event Handlers

        private void HR_Btn_Click(object sender, RoutedEventArgs e)
        {
            this.CallHR();                      
        }

        private void Sales_Btn_Click(object sender, RoutedEventArgs e)
        {
            this.CallSales();
        }

        private void Act_Btn_Click(object sender, RoutedEventArgs e)
        {
            this.CallAccouting();
        }

        private void Dir_Btn_Click(object sender, RoutedEventArgs e)
        {
            SwitchToDirectory();            
        }

        #endregion //Event Handlers

        #region LoadExpectedVisitors

        private void LoadExpectedVisitors()
        {
            // Load from config file or external source. Hardcode for now. 
        }

        #endregion //LoadExpectedVisitors

        public override void SpeechRecognized(SpeechRecognizedEventArgs e)
        {
            string command = e.Result.Semantics.Value.ToString();
            System.Diagnostics.Debug.WriteLine(command);
            switch (command) {
                case "HR":
                    this.Dispatcher.BeginInvoke(new Action(CallHR));
                    break;
                case "SALES":
                    this.Dispatcher.BeginInvoke(new Action(CallSales));
                    break;
                case "ACCOUNTING":
                    this.Dispatcher.BeginInvoke(new Action(CallAccouting));
                    break;
                case "EXPECTED":
                    this.Dispatcher.BeginInvoke(new Action(CallExpected));
                    break;
                case "DIRECTORY":
                    this.Dispatcher.BeginInvoke(new Action(SwitchToDirectory));
                    break;
                default: break;
            }
        }

        public override void SpeechRecognitionRejected(SpeechRecognitionRejectedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Did not understand");
        }

        public override Grammar GetGrammar()
        {
            return homeGrammar;
        }

        public override void GestureCaptured(GestureEventArgs args)
        {
            if (args.Name.Equals("Wave"))
            {
                Console.WriteLine(args.Name + " - Confidence: " + args.Confidence);
            }
        }

        public void SwitchToDirectory()
        {
            TabControl tab = (TabControl)App.Current.MainWindow.FindName("tcMain");
            tab.SelectedIndex = 2;
        }

        private void CallHR()
        {
            Utilities.MakeVideoCall("cshea@infragistics.com", this.HomeGrid.Children, 15, string.Empty);  
        }

        private void CallSales()
        {
            Utilities.MakeVideoCall("cshea@infragistics.com", this.HomeGrid.Children, 15, string.Empty);
        }

        private void CallAccouting()
        {
            Utilities.MakeVideoCall("cshea@infragistics.com", this.HomeGrid.Children, 15, string.Empty);
        }

        private void CallExpected()
        {
            Utilities.MakeVideoCall("cshea@infragistics.com", this.HomeGrid.Children, 15, string.Empty);
        }
    }
}
