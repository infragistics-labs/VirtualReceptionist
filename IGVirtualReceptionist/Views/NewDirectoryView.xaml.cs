using IGVirtualReceptionist.Models;
using IGVirtualReceptionist.ViewModels;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IGVirtualReceptionist.Views
{
    /// <summary>
    /// Interaction logic for NewDirectoryView.xaml
    /// </summary>
    public partial class NewDirectoryView : ViewBase
    {
        private Grammar directoryGrammar;

        public NewDirectoryView()
        {
            InitializeComponent();
            this.DataContext = new DirectoryViewModel();
            GenerateGrammar();
        }

        private void GenerateGrammar()
        {
            DirectoryViewModel vm = this.DataContext as DirectoryViewModel;
            if (vm != null)
            {
                Choices employeeNames = new Choices();

                foreach (SubDirectoryViewModel subDirectory in vm.SubDirectories)
                {
                    foreach (DirectoryEntryModel employee in subDirectory.Entries)
                    {
                        employeeNames.Add(new SemanticResultValue(employee.FullName, employee.FullName));
                        employeeNames.Add(new SemanticResultValue(employee.FirstName, employee.FullName));
                        employeeNames.Add(new SemanticResultValue(employee.LastName, employee.FullName));
                        if (employee.Nickname != "")
                        {
                            employeeNames.Add(new SemanticResultValue(employee.Nickname, employee.FullName));
                        }
                    }
                }

                GrammarBuilder gb = new GrammarBuilder();
                gb.Append(employeeNames);

                directoryGrammar = new Grammar(gb);
            }
        }

        public override Grammar GetGrammar()
        {
            return directoryGrammar;
        }

        public override void SpeechRecognized(SpeechRecognizedEventArgs e)
        {
            string command = e.Result.Semantics.Value.ToString();


            DirectoryViewModel vm = this.DataContext as DirectoryViewModel;
            if (vm != null)
            {
                SubDirectoryViewModel allMatches = vm.SubDirectories.FirstOrDefault(sub => sub.Letter == "All Matches");

                if (allMatches != null)
                {
                    allMatches.Entries.Clear();
                    foreach (RecognizedPhrase match in e.Result.Alternates)
                    {
                        System.Diagnostics.Debug.WriteLine(match.Confidence + " " + match.Text);
                        string matchText = match.Semantics.Value != null ? match.Semantics.Value.ToString() : match.Text;
                        SubDirectoryViewModel matchModel = vm.SubDirectories.FirstOrDefault(sd => sd.Entries.Any(entry => entry.FullName == matchText));
                        if (matchModel != null)
                        {
                            DirectoryEntryModel matchEntry = matchModel.Entries.First(entry => entry.FullName == matchText);

                            if (matchEntry != null)
                            {
                                allMatches.Entries.Add(matchEntry);
                            }
                        }
                    }

                    this.AlphaList.SelectedValue = allMatches;
                }
            }
        }

        public override void SpeechRecognitionRejected(SpeechRecognitionRejectedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Employee failed");
        }

        private void EntryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                DirectoryEntryModel selectedEntry = e.AddedItems[0] as DirectoryEntryModel;

                if (selectedEntry != null)
                {
                    Grid mainWindowGrid = Window.GetWindow(this).FindName("MainWindowGrid") as Grid;
                    if (mainWindowGrid != null)
                    {
                        Utilities.MakeVideoCall(selectedEntry.WorkEmail, mainWindowGrid.Children, 15);
                    }
                }
            }
        }
    }
}
