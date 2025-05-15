using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneLauncher.Views.ViewModels
{ 
    public class BaseViewModel : ObservableObject
    {
        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    if (value)
                        OnNavigatedTo();
                    else
                        OnNavigatedFrom();
                }
            }
        }

        protected virtual void OnNavigatedTo() { }
        protected virtual void OnNavigatedFrom() { }
    }
}
