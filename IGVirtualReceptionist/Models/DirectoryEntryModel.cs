using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace IGVirtualReceptionist.Models
{
    public class DirectoryEntryModelComparer : IComparer<DirectoryEntryModel>
    {
        public int Compare(DirectoryEntryModel x, DirectoryEntryModel y)
        {
            string lName1 = x.LastName == null ? "" : x.LastName;
            string lName2 = y.LastName == null ? "" : y.LastName;
            if (lName1 != lName2)
                return lName1.CompareTo(lName2);

            string fName1 = x.FirstName == null ? "" : x.FirstName;
            string fName2 = y.FirstName == null ? "" : y.FirstName;

            return fName1.CompareTo(fName2);
        }
    }
    public class DirectoryEntryModel
    {
        public DirectoryEntryModel()
        {
        }
        public DirectoryEntryModel(string firstName, string lastName, string emailAddress)
        {
            this.FirstName = firstName;
            this.LastName = lastName;
            this.WorkEmail = emailAddress;
            
        }

        #region Public Properties
        public int ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Nickname { get; set; }
        public string WorkEmail { get; set; }
        public string WorkPhone { get; set; }
        public string WorkExt { get; set; }
        public string Department { get; set; }
        public string CellPhone { get; set; }
        public bool SMS { get; set; }
        public string Title { get; set; }
        public string Division { get; set; }
        public string Location { get; set; }
        public string ReportingTo { get; set; }
        public string Group { get; set; }
        public string Team { get; set; }
        public BitmapImage Photo { get; set; }

        public string FullName
        {
            get
            {
                string full = Nickname != "" ? Nickname : FirstName;
                return full + " " + LastName;
            }
        }

        public string SortLetter
        {
            get
            {
                string retVal = "-";

                if (this.LastName != null && this.LastName.Length > 0)
                    retVal = this.LastName.Substring(0, 1);
                else if (this.Nickname != null && this.Nickname.Length > 0)
                    retVal = this.Nickname.Substring(0, 1);

                return retVal;
            }
        }
        #endregion
    }




}
