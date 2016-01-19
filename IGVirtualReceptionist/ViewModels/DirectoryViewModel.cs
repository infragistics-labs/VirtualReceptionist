using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGVirtualReceptionist.Models;

namespace IGVirtualReceptionist.ViewModels
{
    public class DirectoryViewModel
    {
        private List<DirectoryEntryModel> entriesRaw = null;
        private DirectoryModel model = null;
        public DirectoryViewModel()
        {
            model = new DirectoryModel();
            entriesRaw = model.GetDirectory();

            this.SubDirectories = new List<SubDirectoryViewModel>();
            foreach (string letter in this.AllLetters)
                this.SubDirectories.Add(new SubDirectoryViewModel(letter, model));

            this.SubDirectories.Add(new SubDirectoryViewModel("All Matches", model));
        }

        public List<SubDirectoryViewModel> SubDirectories { get; protected set; }
        #region Public Property : AllLetters
        protected List<string> AllLetters
        {
            get
            {
                return entriesRaw.Select(d => d.LastName == null ||
                                         d.LastName.Length == 0 ?
                                         "-" :
                                         d.LastName.Substring(0, 1)).Distinct().ToList();
            }

        }
        #endregion
    }
}
