using IGVirtualReceptionist.ViewModels;
using System.Windows.Controls;

namespace IGVirtualReceptionist.Views
{
    /// <summary>
    /// Interaction logic for Directory.xaml
    /// </summary>
    public partial class DirectoryView : ViewBase
    {
        public DirectoryView()
        {
            InitializeComponent();
            this.DataContext = new DirectoryViewModel();

        }
    }
}
