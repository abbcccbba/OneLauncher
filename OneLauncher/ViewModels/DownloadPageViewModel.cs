using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ReactiveUI;
namespace OneLauncher.ViewModels
{
    public class DownloadItem
    {
        public string Name { get; set; }
    }
    internal class DownloadPage_ViewModel : BaseViewModel
    {
        private ObservableCollection<DownloadItem> _items;
        public List<string> X { get; set; } = new List<string>() {"1","2"};
    }
}
