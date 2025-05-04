using OneLauncher.Core;
using ReactiveUI;
using System.Reactive;
using System.Windows.Input;

namespace OneLauncher.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private object _currentPage;

        public object CurrentPage
        {
            get => _currentPage;
            set => this.RaiseAndSetIfChanged(ref _currentPage, value);
        }

        public ICommand NavigateToHomeCommand { get; }
        public ICommand NavigateToVersionCommand { get; }
        public ICommand NavigateToAccountCommand { get; }
        public ICommand NavigateToDownloadCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }

        public MainWindowViewModel()
        {
            // 导航命令
            NavigateToHomeCommand = ReactiveCommand.Create(() => CurrentPage = new Home());
            NavigateToVersionCommand = ReactiveCommand.Create(() => CurrentPage = new version());
            NavigateToAccountCommand = ReactiveCommand.Create(() => CurrentPage = new account());
            NavigateToDownloadCommand = ReactiveCommand.Create(() => CurrentPage = new download(Codes.Init.Versions));
            NavigateToSettingsCommand = ReactiveCommand.Create(() => CurrentPage = new settings());
        }

        public void SetInitialPage(VersionsList versions)
        {
            CurrentPage = new Welcome();
        }
    }
}