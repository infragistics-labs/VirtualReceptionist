using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGVirtualReceptionist.Models;
using System.Collections.ObjectModel;

namespace IGVirtualReceptionist.ViewModels
{
    public class SubDirectoryViewModel
    {

        public SubDirectoryViewModel(string letter, DirectoryModel model)
        {
            this.Letter = letter;
            this.Entries = new ObservableCollection<DirectoryEntryModel>(model.GetDirectory().Where(d => d.SortLetter == letter));
        }

        public string Letter { get; protected set; }
        public ObservableCollection<DirectoryEntryModel> Entries { get; protected set; }

    }
}
